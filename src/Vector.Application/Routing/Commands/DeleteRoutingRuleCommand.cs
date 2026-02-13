using MediatR;
using Vector.Domain.Common;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Command to delete (archive) a routing rule.
/// </summary>
public sealed record DeleteRoutingRuleCommand(Guid Id) : IRequest<Result>;
