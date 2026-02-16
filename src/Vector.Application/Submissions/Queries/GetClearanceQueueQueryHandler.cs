using MediatR;
using Vector.Application.Common.Interfaces;
using Vector.Application.Submissions.DTOs;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Enums;

namespace Vector.Application.Submissions.Queries;

/// <summary>
/// Handler for retrieving the clearance review queue.
/// </summary>
public sealed class GetClearanceQueueQueryHandler(
    ISubmissionRepository submissionRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetClearanceQueueQuery, IReadOnlyList<SubmissionSummaryDto>>
{
    public async Task<IReadOnlyList<SubmissionSummaryDto>> Handle(
        GetClearanceQueueQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = currentUserService.TenantId
            ?? throw new InvalidOperationException("Tenant ID is required");

        var submissions = await submissionRepository.GetByStatusAsync(
            tenantId,
            SubmissionStatus.PendingClearance,
            request.Limit,
            cancellationToken);

        return submissions.Select(s => new SubmissionSummaryDto(
            s.Id,
            s.SubmissionNumber,
            s.Insured.Name,
            s.Status.ToString(),
            s.ReceivedAt,
            s.EffectiveDate,
            s.AssignedUnderwriterName,
            s.AppetiteScore,
            s.WinnabilityScore,
            s.DataQualityScore,
            s.Coverages.Count,
            s.Locations.Count,
            s.Locations.Sum(l => l.TotalInsuredValue.Amount),
            s.ClearanceStatus.ToString())).ToList();
    }
}
