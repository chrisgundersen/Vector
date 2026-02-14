using MediatR;
using Vector.Application.Common.Interfaces;
using Vector.Application.Submissions.DTOs;
using Vector.Domain.Submission;

namespace Vector.Application.Submissions.Queries;

/// <summary>
/// Handler for retrieving a submission by its submission number.
/// </summary>
public sealed class GetSubmissionByNumberQueryHandler(
    ISubmissionRepository submissionRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetSubmissionByNumberQuery, SubmissionDto?>
{
    public async Task<SubmissionDto?> Handle(
        GetSubmissionByNumberQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = currentUserService.TenantId
            ?? throw new InvalidOperationException("Tenant ID is required");

        var submission = await submissionRepository.GetBySubmissionNumberAsync(
            tenantId,
            request.SubmissionNumber,
            cancellationToken);

        return submission?.ToDto();
    }
}
