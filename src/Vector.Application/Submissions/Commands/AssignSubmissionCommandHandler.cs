using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Domain.Common;
using Vector.Domain.Submission;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Handler for AssignSubmissionCommand.
/// </summary>
public sealed class AssignSubmissionCommandHandler(
    ISubmissionRepository submissionRepository,
    ILogger<AssignSubmissionCommandHandler> logger) : IRequestHandler<AssignSubmissionCommand, Result>
{
    public async Task<Result> Handle(
        AssignSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        var submission = await submissionRepository.GetByIdAsync(request.SubmissionId, cancellationToken);

        if (submission is null)
        {
            return Result.Failure(new Error(
                "Submission.NotFound",
                $"Submission with ID {request.SubmissionId} was not found."));
        }

        var result = submission.AssignToUnderwriter(request.UnderwriterId, request.UnderwriterName);

        if (result.IsFailure)
        {
            return result;
        }

        submissionRepository.Update(submission);

        logger.LogInformation(
            "Assigned submission {SubmissionId} to underwriter {UnderwriterName}",
            request.SubmissionId, request.UnderwriterName);

        return Result.Success();
    }
}
