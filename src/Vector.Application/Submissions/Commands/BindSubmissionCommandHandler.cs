using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;
using Vector.Domain.Submission;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Handler for BindSubmissionCommand.
/// Binds the submission and integrates with external PAS.
/// </summary>
public sealed class BindSubmissionCommandHandler(
    ISubmissionRepository submissionRepository,
    IExternalPolicyService externalPolicyService,
    IExternalCrmService externalCrmService,
    ILogger<BindSubmissionCommandHandler> logger) : IRequestHandler<BindSubmissionCommand, Result<BindSubmissionResult>>
{
    public async Task<Result<BindSubmissionResult>> Handle(
        BindSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        var submission = await submissionRepository.GetByIdAsync(request.SubmissionId, cancellationToken);

        if (submission is null)
        {
            return Result.Failure<BindSubmissionResult>(new Error(
                "Submission.NotFound",
                $"Submission with ID {request.SubmissionId} was not found."));
        }

        // Bind the submission in domain
        var bindResult = submission.Bind();
        if (bindResult.IsFailure)
        {
            return Result.Failure<BindSubmissionResult>(bindResult.Error);
        }

        submissionRepository.Update(submission);

        // Sync customer to CRM
        await SyncCustomerToCrmAsync(submission, cancellationToken);

        // Create policy in external PAS
        string? externalPolicyId = null;
        string? policyNumber = null;

        var pasResult = await CreatePolicyInPasAsync(submission, cancellationToken);
        if (pasResult.IsSuccess && pasResult.Value is not null)
        {
            externalPolicyId = pasResult.Value.ExternalPolicyId;
            policyNumber = pasResult.Value.PolicyNumber;

            logger.LogInformation(
                "Created policy {PolicyNumber} in external PAS for submission {SubmissionNumber}",
                policyNumber, submission.SubmissionNumber);
        }
        else
        {
            logger.LogWarning(
                "Failed to create policy in external PAS for submission {SubmissionNumber}: {Error}",
                submission.SubmissionNumber, pasResult.Error?.Description ?? "Unknown error");
        }

        // Record activity in CRM
        await RecordBindActivityInCrmAsync(submission, policyNumber, cancellationToken);

        logger.LogInformation(
            "Bound submission {SubmissionNumber}",
            submission.SubmissionNumber);

        return Result.Success(new BindSubmissionResult(
            submission.Id,
            submission.SubmissionNumber,
            externalPolicyId,
            policyNumber,
            DateTime.UtcNow));
    }

    private async Task SyncCustomerToCrmAsync(
        Domain.Submission.Aggregates.Submission submission,
        CancellationToken cancellationToken)
    {
        try
        {
            var customerRequest = new CustomerSyncRequest(
                InternalCustomerId: submission.Insured.Id,
                ExternalCustomerId: null,
                CustomerName: submission.Insured.Name,
                DbaName: submission.Insured.DbaName,
                FeinNumber: submission.Insured.FeinNumber,
                Industry: submission.Insured.Industry?.ToString(),
                ContactName: null,
                ContactEmail: null,
                ContactPhone: null,
                Address: submission.Insured.MailingAddress is not null
                    ? new AddressInfo(
                        submission.Insured.MailingAddress.Street1,
                        submission.Insured.MailingAddress.Street2,
                        submission.Insured.MailingAddress.City,
                        submission.Insured.MailingAddress.State,
                        submission.Insured.MailingAddress.PostalCode,
                        submission.Insured.MailingAddress.Country)
                    : null,
                AdditionalData: null);

            await externalCrmService.SyncCustomerAsync(customerRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to sync customer to CRM for submission {SubmissionNumber}",
                submission.SubmissionNumber);
        }
    }

    private async Task<Result<PolicyCreationResult>> CreatePolicyInPasAsync(
        Domain.Submission.Aggregates.Submission submission,
        CancellationToken cancellationToken)
    {
        try
        {
            var coverages = submission.Coverages.Select(c => new PolicyCoverageInfo(
                CoverageType: c.Type.ToString(),
                Limit: c.RequestedLimit?.Amount ?? 0,
                Deductible: c.RequestedDeductible?.Amount ?? 0,
                Premium: null)).ToList();

            var locations = submission.Locations.Select(l => new PolicyLocationInfo(
                LocationNumber: l.LocationNumber,
                Address: new AddressInfo(
                    l.Address.Street1,
                    l.Address.Street2,
                    l.Address.City,
                    l.Address.State,
                    l.Address.PostalCode,
                    l.Address.Country),
                BuildingDescription: l.BuildingDescription,
                OccupancyType: l.OccupancyType,
                ConstructionType: l.ConstructionType?.ToString(),
                YearBuilt: l.YearBuilt,
                BuildingValue: l.BuildingValue?.Amount ?? 0,
                ContentsValue: l.ContentsValue?.Amount ?? 0,
                BusinessIncomeValue: l.BusinessIncomeValue?.Amount ?? 0,
                TotalInsuredValue: l.TotalInsuredValue.Amount)).ToList();

            var request = new PolicyCreationRequest(
                SubmissionId: submission.Id,
                SubmissionNumber: submission.SubmissionNumber,
                InsuredName: submission.Insured.Name,
                InsuredFein: submission.Insured.FeinNumber,
                InsuredAddress: submission.Insured.MailingAddress is not null
                    ? new AddressInfo(
                        submission.Insured.MailingAddress.Street1,
                        submission.Insured.MailingAddress.Street2,
                        submission.Insured.MailingAddress.City,
                        submission.Insured.MailingAddress.State,
                        submission.Insured.MailingAddress.PostalCode,
                        submission.Insured.MailingAddress.Country)
                    : null,
                EffectiveDate: submission.EffectiveDate ?? DateTime.UtcNow,
                ExpirationDate: submission.ExpirationDate ?? DateTime.UtcNow.AddYears(1),
                Premium: submission.QuotedPremium?.Amount ?? 0,
                Currency: submission.QuotedPremium?.Currency ?? "USD",
                Coverages: coverages,
                Locations: locations,
                ProducerCode: null,
                UnderwriterCode: null,
                AdditionalData: null);

            return await externalPolicyService.CreatePolicyAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Exception creating policy in PAS for submission {SubmissionNumber}",
                submission.SubmissionNumber);

            return Result.Failure<PolicyCreationResult>(new Error(
                "PAS.Exception",
                $"Failed to create policy: {ex.Message}"));
        }
    }

    private async Task RecordBindActivityInCrmAsync(
        Domain.Submission.Aggregates.Submission submission,
        string? policyNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var activity = new CrmActivityRequest(
                ExternalProducerId: null,
                ExternalCustomerId: null,
                ActivityType: "PolicyBound",
                Subject: $"Policy Bound: {submission.SubmissionNumber}",
                Description: $"Submission {submission.SubmissionNumber} for {submission.Insured.Name} has been bound. " +
                             $"Policy Number: {policyNumber ?? "Pending"}. " +
                             $"Effective: {submission.EffectiveDate:d}.",
                ActivityDate: DateTime.UtcNow,
                ReferenceNumber: submission.SubmissionNumber,
                AdditionalData: null);

            await externalCrmService.RecordActivityAsync(activity, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to record bind activity in CRM for submission {SubmissionNumber}",
                submission.SubmissionNumber);
        }
    }
}
