using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Domain.Common;
using Vector.Domain.Submission;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Handler for RequestInformationCommand.
/// </summary>
public sealed class RequestInformationCommandHandler(
    ISubmissionRepository submissionRepository,
    ILogger<RequestInformationCommandHandler> logger) : IRequestHandler<RequestInformationCommand, Result>
{
    public async Task<Result> Handle(
        RequestInformationCommand request,
        CancellationToken cancellationToken)
    {
        var submission = await submissionRepository.GetByIdAsync(request.SubmissionId, cancellationToken);

        if (submission is null)
        {
            return Result.Failure(new Error(
                "Submission.NotFound",
                $"Submission with ID {request.SubmissionId} was not found."));
        }

        var result = submission.RequestInformation(request.Reason);

        if (result.IsFailure)
        {
            return result;
        }

        submissionRepository.Update(submission);

        logger.LogInformation(
            "Requested information for submission {SubmissionId}: {Reason}",
            request.SubmissionId, request.Reason);

        return Result.Success();
    }
}
