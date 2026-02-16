using MediatR;
using Vector.Domain.Common;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Command to update an existing producer-underwriter pairing.
/// </summary>
public sealed record UpdatePairingCommand(
    Guid Id,
    int Priority,
    DateTime EffectiveFrom,
    DateTime? EffectiveUntil,
    IReadOnlyList<string>? CoverageTypes) : IRequest<Result>;
