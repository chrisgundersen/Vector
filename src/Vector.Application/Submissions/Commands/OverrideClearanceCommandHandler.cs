using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Domain.Common;
using Vector.Domain.Submission;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Handler for OverrideClearanceCommand.
/// </summary>
public sealed class OverrideClearanceCommandHandler(
    ISubmissionRepository submissionRepository,
    ILogger<OverrideClearanceCommandHandler> logger) : IRequestHandler<OverrideClearanceCommand, Result>
{
    public async Task<Result> Handle(
        OverrideClearanceCommand request,
        CancellationToken cancellationToken)
    {
        var submission = await submissionRepository.GetByIdAsync(request.SubmissionId, cancellationToken);

        if (submission is null)
        {
            return Result.Failure(new Error(
                "Submission.NotFound",
                $"Submission with ID {request.SubmissionId} was not found."));
        }

        var result = submission.OverrideClearance(request.Reason, request.OverriddenByUserId);

        if (result.IsFailure)
        {
            return result;
        }

        submissionRepository.Update(submission);

        logger.LogInformation(
            "Clearance overridden for submission {SubmissionId} by user {UserId} with reason: {Reason}",
            request.SubmissionId, request.OverriddenByUserId, request.Reason);

        return Result.Success();
    }
}
