using MediatR;
using Vector.Domain.Common;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Command to deactivate a producer-underwriter pairing.
/// </summary>
public sealed record DeactivatePairingCommand(Guid Id) : IRequest<Result>;
