using MediatR;
using Vector.Application.Common.Interfaces;
using Vector.Application.UnderwritingGuidelines.DTOs;
using Vector.Domain.UnderwritingGuidelines;

namespace Vector.Application.UnderwritingGuidelines.Queries;

/// <summary>
/// Handler for GetGuidelinesQuery.
/// </summary>
public sealed class GetGuidelinesQueryHandler(
    IUnderwritingGuidelineRepository repository,
    ICurrentUserService currentUserService) : IRequestHandler<GetGuidelinesQuery, IReadOnlyList<GuidelineSummaryDto>>
{
    public async Task<IReadOnlyList<GuidelineSummaryDto>> Handle(
        GetGuidelinesQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = currentUserService.TenantId
            ?? throw new InvalidOperationException("Tenant ID is required");

        IReadOnlyList<Domain.UnderwritingGuidelines.Aggregates.UnderwritingGuideline> guidelines;

        if (request.Status.HasValue)
        {
            guidelines = await repository.GetByStatusAsync(tenantId, request.Status.Value, cancellationToken);
        }
        else
        {
            guidelines = await repository.GetActiveForTenantAsync(tenantId, cancellationToken);
        }

        return guidelines.Select(g => g.ToSummaryDto()).ToList();
    }
}
