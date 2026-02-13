using MediatR;
using Vector.Domain.Common;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Command to deactivate a routing rule.
/// </summary>
public sealed record DeactivateRoutingRuleCommand(Guid Id) : IRequest<Result>;
