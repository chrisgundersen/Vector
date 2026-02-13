using MediatR;
using Vector.Domain.Common;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Command to activate a producer-underwriter pairing.
/// </summary>
public sealed record ActivatePairingCommand(Guid Id) : IRequest<Result>;
