using MediatR;
using Vector.Domain.Common;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Command to activate a routing rule.
/// </summary>
public sealed record ActivateRoutingRuleCommand(Guid Id) : IRequest<Result>;
