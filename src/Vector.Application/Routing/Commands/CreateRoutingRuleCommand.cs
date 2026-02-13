using MediatR;
using Vector.Domain.Common;
using Vector.Domain.Routing.Enums;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Command to create a new routing rule.
/// </summary>
public sealed record CreateRoutingRuleCommand(
    string Name,
    string Description,
    RoutingStrategy Strategy,
    int Priority,
    Guid? TargetUnderwriterId,
    string? TargetUnderwriterName,
    Guid? TargetTeamId,
    string? TargetTeamName) : IRequest<Result<Guid>>;
