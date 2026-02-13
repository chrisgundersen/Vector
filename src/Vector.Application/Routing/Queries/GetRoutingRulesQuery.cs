using MediatR;
using Vector.Application.Routing.DTOs;
using Vector.Domain.Routing.Enums;

namespace Vector.Application.Routing.Queries;

/// <summary>
/// Query to get all routing rules, optionally filtered by status.
/// </summary>
public sealed record GetRoutingRulesQuery(
    RoutingRuleStatus? Status = null) : IRequest<IReadOnlyList<RoutingRuleSummaryDto>>;
