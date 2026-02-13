using MediatR;
using Vector.Application.Common.Interfaces;
using Vector.Application.Submissions.DTOs;
using Vector.Domain.Submission;

namespace Vector.Application.Submissions.Queries;

/// <summary>
/// Handler for retrieving producer submissions with filtering and pagination.
/// </summary>
public sealed class GetProducerSubmissionsQueryHandler(
    ISubmissionRepository submissionRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetProducerSubmissionsQuery, ProducerSubmissionsResult>
{
    public async Task<ProducerSubmissionsResult> Handle(
        GetProducerSubmissionsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = currentUserService.TenantId
            ?? throw new InvalidOperationException("Tenant ID is required");

        var (submissions, totalCount) = await submissionRepository.SearchAsync(
            tenantId,
            request.ProducerId,
            request.Status,
            request.SearchTerm,
            request.Page,
            request.PageSize,
            cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var summaries = submissions.Select(s => new SubmissionSummaryDto(
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
            s.Locations.Sum(l => l.TotalInsuredValue.Amount))).ToList();

        return new ProducerSubmissionsResult(
            summaries,
            totalCount,
            request.Page,
            request.PageSize,
            totalPages);
    }
}
