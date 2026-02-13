using Microsoft.EntityFrameworkCore;
using Vector.Domain.Routing;
using Vector.Domain.Routing.Entities;
using Vector.Domain.Submission.Enums;

namespace Vector.Infrastructure.Persistence.Repositories;

public class ProducerUnderwriterPairingRepository(VectorDbContext context) : IProducerUnderwriterPairingRepository
{
    public async Task<ProducerUnderwriterPairing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.ProducerUnderwriterPairings
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProducerUnderwriterPairing>> GetByProducerIdAsync(
        Guid producerId,
        CancellationToken cancellationToken = default)
    {
        return await context.ProducerUnderwriterPairings
            .Where(p => p.ProducerId == producerId)
            .OrderBy(p => p.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProducerUnderwriterPairing>> GetByUnderwriterIdAsync(
        Guid underwriterId,
        CancellationToken cancellationToken = default)
    {
        return await context.ProducerUnderwriterPairings
            .Where(p => p.UnderwriterId == underwriterId)
            .OrderBy(p => p.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProducerUnderwriterPairing>> GetActivePairingsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await context.ProducerUnderwriterPairings
            .Where(p => p.IsActive &&
                       p.EffectiveFrom <= now &&
                       (!p.EffectiveUntil.HasValue || p.EffectiveUntil >= now))
            .OrderBy(p => p.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProducerUnderwriterPairing?> FindMatchingPairingAsync(
        Guid producerId,
        CoverageType coverageType,
        DateTime asOf,
        CancellationToken cancellationToken = default)
    {
        // Get all potentially matching pairings for this producer
        var pairings = await context.ProducerUnderwriterPairings
            .Where(p => p.ProducerId == producerId &&
                       p.IsActive &&
                       p.EffectiveFrom <= asOf &&
                       (!p.EffectiveUntil.HasValue || p.EffectiveUntil >= asOf))
            .OrderBy(p => p.Priority)
            .ToListAsync(cancellationToken);

        // Find first that matches coverage type (in-memory check due to JSON storage)
        return pairings.FirstOrDefault(p => p.AppliesToCoverage(coverageType));
    }

    public async Task AddAsync(ProducerUnderwriterPairing pairing, CancellationToken cancellationToken = default)
    {
        await context.ProducerUnderwriterPairings.AddAsync(pairing, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProducerUnderwriterPairing pairing, CancellationToken cancellationToken = default)
    {
        context.ProducerUnderwriterPairings.Update(pairing);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var pairing = await GetByIdAsync(id, cancellationToken);
        if (pairing is not null)
        {
            context.ProducerUnderwriterPairings.Remove(pairing);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
