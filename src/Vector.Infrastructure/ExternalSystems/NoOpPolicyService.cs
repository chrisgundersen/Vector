using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;

namespace Vector.Infrastructure.ExternalSystems;

/// <summary>
/// No-op implementation of IExternalPolicyService for development and testing.
/// Logs operations but does not integrate with any real PAS.
/// </summary>
public sealed class NoOpPolicyService(ILogger<NoOpPolicyService> logger) : IExternalPolicyService
{
    public Task<Result<PolicyCreationResult>> CreatePolicyAsync(
        PolicyCreationRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "NoOp: Would create policy for submission {SubmissionNumber} with premium {Premium:C}",
            request.SubmissionNumber, request.Premium);

        var result = new PolicyCreationResult(
            ExternalPolicyId: $"NOOP-{Guid.NewGuid():N}",
            PolicyNumber: $"POL-{request.SubmissionNumber}",
            CreatedAt: DateTime.UtcNow,
            ExternalSystemReference: "NoOp-PAS-Simulator");

        return Task.FromResult(Result.Success(result));
    }

    public Task<Result> UpdatePolicyAsync(
        string externalPolicyId,
        PolicyUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "NoOp: Would update policy {ExternalPolicyId}",
            externalPolicyId);

        return Task.FromResult(Result.Success());
    }

    public Task<Result<ExternalPolicyStatus>> GetPolicyStatusAsync(
        string externalPolicyId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "NoOp: Would get status for policy {ExternalPolicyId}",
            externalPolicyId);

        var status = new ExternalPolicyStatus(
            ExternalPolicyId: externalPolicyId,
            PolicyNumber: $"POL-{externalPolicyId[5..13]}",
            Status: "Active",
            EffectiveDate: DateTime.UtcNow.AddDays(-30),
            ExpirationDate: DateTime.UtcNow.AddDays(335),
            CancellationDate: null,
            WrittenPremium: 25000m,
            LastUpdated: DateTime.UtcNow);

        return Task.FromResult(Result.Success(status));
    }

    public Task<Result> CancelPolicyAsync(
        string externalPolicyId,
        string reason,
        DateTime effectiveDate,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "NoOp: Would cancel policy {ExternalPolicyId} effective {EffectiveDate} for reason: {Reason}",
            externalPolicyId, effectiveDate, reason);

        return Task.FromResult(Result.Success());
    }
}
