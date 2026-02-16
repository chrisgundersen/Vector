using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Domain.Common;
using Vector.Domain.Submission;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Handler for ExpireSubmissionCommand.
/// </summary>
public sealed class ExpireSubmissionCommandHandler(
    ISubmissionRepository submissionRepository,
    ILogger<ExpireSubmissionCommandHandler> logger) : IRequestHandler<ExpireSubmissionCommand, Result>
{
    public async Task<Result> Handle(
        ExpireSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        var submission = await submissionRepository.GetByIdAsync(request.SubmissionId, cancellationToken);

        if (submission is null)
        {
            return Result.Failure(new Error(
                "Submission.NotFound",
                $"Submission with ID {request.SubmissionId} was not found."));
        }

        var result = submission.Expire(request.Reason);

        if (result.IsFailure)
        {
            return result;
        }

        submissionRepository.Update(submission);

        logger.LogInformation(
            "Expired submission {SubmissionId} with reason: {Reason}",
            request.SubmissionId, request.Reason);

        return Result.Success();
    }
}
