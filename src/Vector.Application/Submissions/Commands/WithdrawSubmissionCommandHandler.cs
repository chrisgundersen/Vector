using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Domain.Common;
using Vector.Domain.Submission;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Handler for WithdrawSubmissionCommand.
/// </summary>
public sealed class WithdrawSubmissionCommandHandler(
    ISubmissionRepository submissionRepository,
    ILogger<WithdrawSubmissionCommandHandler> logger) : IRequestHandler<WithdrawSubmissionCommand, Result>
{
    public async Task<Result> Handle(
        WithdrawSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        var submission = await submissionRepository.GetByIdAsync(request.SubmissionId, cancellationToken);

        if (submission is null)
        {
            return Result.Failure(new Error(
                "Submission.NotFound",
                $"Submission with ID {request.SubmissionId} was not found."));
        }

        var result = submission.Withdraw(request.Reason ?? "Withdrawn");

        if (result.IsFailure)
        {
            return result;
        }

        submissionRepository.Update(submission);

        logger.LogInformation(
            "Withdrawn submission {SubmissionId} with reason: {Reason}",
            request.SubmissionId, request.Reason);

        return Result.Success();
    }
}
