using MediatR;
using Vector.Domain.Common;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Command to create a new producer-underwriter pairing.
/// </summary>
public sealed record CreatePairingCommand(
    Guid ProducerId,
    string ProducerName,
    Guid UnderwriterId,
    string UnderwriterName,
    int Priority,
    DateTime EffectiveFrom,
    DateTime? EffectiveUntil,
    IReadOnlyList<string>? CoverageTypes) : IRequest<Result<Guid>>;
