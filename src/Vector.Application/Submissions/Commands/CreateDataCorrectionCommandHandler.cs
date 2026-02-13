using MediatR;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Entities;

namespace Vector.Application.Submissions.Commands;

/// <summary>
/// Handler for creating data correction requests.
/// </summary>
public sealed class CreateDataCorrectionCommandHandler(
    IDataCorrectionRepository correctionRepository,
    ISubmissionRepository submissionRepository,
    ICurrentUserService currentUserService) : IRequestHandler<CreateDataCorrectionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateDataCorrectionCommand request,
        CancellationToken cancellationToken)
    {
        // Verify submission exists
        var submission = await submissionRepository.GetByIdAsync(request.SubmissionId, cancellationToken);
        if (submission is null)
        {
            return Result.Failure<Guid>(new Error("Submission.NotFound", "Submission not found"));
        }

        // Create the correction request
        var correction = DataCorrectionRequest.Create(
            request.SubmissionId,
            request.Type,
            request.FieldName,
            request.CurrentValue,
            request.ProposedValue,
            request.Justification,
            currentUserService.UserName);

        await correctionRepository.AddAsync(correction, cancellationToken);

        return Result.Success(correction.Id);
    }
}
