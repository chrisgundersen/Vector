using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Domain.Common;
using Vector.Domain.Submission;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Handler for DeclineSubmissionCommand.
/// </summary>
public sealed class DeclineSubmissionCommandHandler(
    ISubmissionRepository submissionRepository,
    ILogger<DeclineSubmissionCommandHandler> logger) : IRequestHandler<DeclineSubmissionCommand, Result>
{
    public async Task<Result> Handle(
        DeclineSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        var submission = await submissionRepository.GetByIdAsync(request.SubmissionId, cancellationToken);

        if (submission is null)
        {
            return Result.Failure(new Error(
                "Submission.NotFound",
                $"Submission with ID {request.SubmissionId} was not found."));
        }

        var result = submission.Decline(request.Reason);

        if (result.IsFailure)
        {
            return result;
        }

        submissionRepository.Update(submission);

        logger.LogInformation(
            "Declined submission {SubmissionId} with reason: {Reason}",
            request.SubmissionId, request.Reason);

        return Result.Success();
    }
}
