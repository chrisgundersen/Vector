using MediatR;
using Vector.Domain.Common;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Command to delete a producer-underwriter pairing.
/// </summary>
public sealed record DeletePairingCommand(Guid Id) : IRequest<Result>;
