using MediatR;
using Vector.Application.Routing.DTOs;
using Vector.Domain.Routing;
using Vector.Domain.Routing.Aggregates;

namespace Vector.Application.Routing.Queries;

/// <summary>
/// Handler for GetRoutingRulesQuery.
/// </summary>
public sealed class GetRoutingRulesQueryHandler(
    IRoutingRuleRepository repository) : IRequestHandler<GetRoutingRulesQuery, IReadOnlyList<RoutingRuleSummaryDto>>
{
    public async Task<IReadOnlyList<RoutingRuleSummaryDto>> Handle(
        GetRoutingRulesQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<RoutingRule> rules;

        if (request.Status.HasValue)
        {
            rules = await repository.GetByStatusAsync(request.Status.Value, cancellationToken);
        }
        else
        {
            rules = await repository.GetActiveRulesAsync(cancellationToken);
        }

        return rules.Select(r => r.ToSummaryDto()).ToList();
    }
}
