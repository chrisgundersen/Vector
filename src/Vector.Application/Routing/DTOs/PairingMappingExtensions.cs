using Vector.Domain.Routing.Entities;

namespace Vector.Application.Routing.DTOs;

public static class PairingMappingExtensions
{
    public static PairingSummaryDto ToSummaryDto(this ProducerUnderwriterPairing pairing) =>
        new(
            pairing.Id,
            pairing.ProducerId,
            pairing.ProducerName,
            pairing.UnderwriterId,
            pairing.UnderwriterName,
            pairing.Priority,
            pairing.IsActive,
            pairing.EffectiveFrom,
            pairing.EffectiveUntil,
            pairing.CoverageTypes.Count);

    public static PairingDto ToDto(this ProducerUnderwriterPairing pairing) =>
        new(
            pairing.Id,
            pairing.ProducerId,
            pairing.ProducerName,
            pairing.UnderwriterId,
            pairing.UnderwriterName,
            pairing.Priority,
            pairing.IsActive,
            pairing.EffectiveFrom,
            pairing.EffectiveUntil,
            pairing.CoverageTypes.Select(ct => ct.ToString()).ToList());
}
