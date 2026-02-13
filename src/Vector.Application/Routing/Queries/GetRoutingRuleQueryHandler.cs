using MediatR;
using Vector.Application.Routing.DTOs;
using Vector.Domain.Routing;

namespace Vector.Application.Routing.Queries;

/// <summary>
/// Handler for GetRoutingRuleQuery.
/// </summary>
public sealed class GetRoutingRuleQueryHandler(
    IRoutingRuleRepository repository) : IRequestHandler<GetRoutingRuleQuery, RoutingRuleDto?>
{
    public async Task<RoutingRuleDto?> Handle(
        GetRoutingRuleQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await repository.GetByIdAsync(request.Id, cancellationToken);
        return rule?.ToDto();
    }
}
