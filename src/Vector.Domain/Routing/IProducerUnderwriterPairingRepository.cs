using Vector.Domain.Routing.Entities;
using Vector.Domain.Submission.Enums;

namespace Vector.Domain.Routing;

/// <summary>
/// Repository interface for producer-underwriter pairings.
/// </summary>
public interface IProducerUnderwriterPairingRepository
{
    Task<ProducerUnderwriterPairing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProducerUnderwriterPairing>> GetByProducerIdAsync(Guid producerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProducerUnderwriterPairing>> GetByUnderwriterIdAsync(Guid underwriterId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProducerUnderwriterPairing>> GetActivePairingsAsync(CancellationToken cancellationToken = default);
    Task<ProducerUnderwriterPairing?> FindMatchingPairingAsync(
        Guid producerId,
        CoverageType coverageType,
        DateTime asOf,
        CancellationToken cancellationToken = default);
    Task AddAsync(ProducerUnderwriterPairing pairing, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProducerUnderwriterPairing pairing, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
