namespace Vector.Application.Routing.DTOs;

/// <summary>
/// Summary DTO for producer-underwriter pairing listings.
/// </summary>
public sealed record PairingSummaryDto(
    Guid Id,
    Guid ProducerId,
    string ProducerName,
    Guid UnderwriterId,
    string UnderwriterName,
    int Priority,
    bool IsActive,
    DateTime EffectiveFrom,
    DateTime? EffectiveUntil,
    int CoverageTypeCount);

/// <summary>
/// Detailed DTO for a single producer-underwriter pairing.
/// </summary>
public sealed record PairingDto(
    Guid Id,
    Guid ProducerId,
    string ProducerName,
    Guid UnderwriterId,
    string UnderwriterName,
    int Priority,
    bool IsActive,
    DateTime EffectiveFrom,
    DateTime? EffectiveUntil,
    IReadOnlyList<string> CoverageTypes);
