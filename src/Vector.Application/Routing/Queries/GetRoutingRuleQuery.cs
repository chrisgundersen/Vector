using MediatR;
using Vector.Application.Routing.DTOs;

namespace Vector.Application.Routing.Queries;

/// <summary>
/// Query to get a routing rule by ID.
/// </summary>
public sealed record GetRoutingRuleQuery(Guid Id) : IRequest<RoutingRuleDto?>;
