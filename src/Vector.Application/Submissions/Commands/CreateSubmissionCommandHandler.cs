using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Domain.Common;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Services;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Handler for CreateSubmissionCommand.
/// </summary>
public sealed class CreateSubmissionCommandHandler(
    ISubmissionRepository submissionRepository,
    IClearanceCheckService clearanceCheckService,
    ILogger<CreateSubmissionCommandHandler> logger) : IRequestHandler<CreateSubmissionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        var submissionNumber = await submissionRepository.GenerateSubmissionNumberAsync(
            request.TenantId,
            cancellationToken);

        var submissionResult = Domain.Submission.Aggregates.Submission.Create(
            request.TenantId,
            submissionNumber,
            request.InsuredName,
            request.ProcessingJobId,
            request.InboundEmailId);

        if (submissionResult.IsFailure)
        {
            return Result.Failure<Guid>(submissionResult.Error);
        }

        var submission = submissionResult.Value;
        submission.MarkAsReceived();

        try
        {
            var matches = await clearanceCheckService.CheckAsync(submission, cancellationToken);
            submission.CompleteClearance(matches);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Clearance check failed for submission {SubmissionNumber}. Submission will require manual review",
                submissionNumber);
            submission.CompleteClearance([]);
        }

        await submissionRepository.AddAsync(submission, cancellationToken);

        logger.LogInformation(
            "Created submission {SubmissionNumber} for {InsuredName} in tenant {TenantId} with clearance status {ClearanceStatus}",
            submissionNumber, request.InsuredName, request.TenantId, submission.ClearanceStatus);

        return Result.Success(submission.Id);
    }
}
