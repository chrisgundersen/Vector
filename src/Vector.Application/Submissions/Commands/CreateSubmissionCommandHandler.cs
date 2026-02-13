using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Domain.Common;
using Vector.Domain.Submission;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Handler for CreateSubmissionCommand.
/// </summary>
public sealed class CreateSubmissionCommandHandler(
    ISubmissionRepository submissionRepository,
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

        await submissionRepository.AddAsync(submission, cancellationToken);

        logger.LogInformation(
            "Created submission {SubmissionNumber} for {InsuredName} in tenant {TenantId}",
            submissionNumber, request.InsuredName, request.TenantId);

        return Result.Success(submission.Id);
    }
}
