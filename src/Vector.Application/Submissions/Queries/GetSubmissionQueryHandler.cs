using MediatR;
using Vector.Application.Submissions.DTOs;
using Vector.Domain.Submission;

namespace Vector.Application.Submissions.Queries;

/// <summary>
/// Handler for GetSubmissionQuery.
/// </summary>
public sealed class GetSubmissionQueryHandler(
    ISubmissionRepository submissionRepository) : IRequestHandler<GetSubmissionQuery, SubmissionDto?>
{
    public async Task<SubmissionDto?> Handle(
        GetSubmissionQuery request,
        CancellationToken cancellationToken)
    {
        var submission = await submissionRepository.GetByIdAsync(request.SubmissionId, cancellationToken);

        return submission?.ToDto();
    }
}
