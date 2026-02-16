using Microsoft.EntityFrameworkCore;
using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines;
using Vector.Domain.UnderwritingGuidelines.Aggregates;
using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Infrastructure.Persistence.Repositories;

public class UnderwritingGuidelineRepository(VectorDbContext context) : IUnderwritingGuidelineRepository
{
    public IUnitOfWork UnitOfWork => context;

    public async Task<UnderwritingGuideline?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await context.UnderwritingGuidelines
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<UnderwritingGuideline?> GetByIdWithRulesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await context.UnderwritingGuidelines
            .Include(g => g.Rules)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<UnderwritingGuideline>> GetActiveForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await context.UnderwritingGuidelines
            .Include(g => g.Rules)
            .Where(g => g.TenantId == tenantId && g.Status == GuidelineStatus.Active)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UnderwritingGuideline>> GetByStatusAsync(
        Guid tenantId,
        GuidelineStatus status,
        CancellationToken cancellationToken = default)
    {
        return await context.UnderwritingGuidelines
            .Include(g => g.Rules)
            .Where(g => g.TenantId == tenantId && g.Status == status)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UnderwritingGuideline>> GetApplicableGuidelinesAsync(
        Guid tenantId,
        string? coverageType = null,
        string? state = null,
        string? naicsCode = null,
        CancellationToken cancellationToken = default)
    {
        var guidelines = await context.UnderwritingGuidelines
            .Include(g => g.Rules)
            .Where(g => g.TenantId == tenantId && g.Status == GuidelineStatus.Active)
            .ToListAsync(cancellationToken);

        // Filter applicable guidelines (domain logic)
        return guidelines
            .Where(g => g.IsApplicable(coverageType, state, naicsCode))
            .ToList();
    }

    public async Task<bool> ExistsByNameAsync(
        Guid tenantId,
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.UnderwritingGuidelines
            .Where(g => g.TenantId == tenantId && g.Name == name);

        if (excludeId.HasValue)
        {
            query = query.Where(g => g.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(
        UnderwritingGuideline aggregate,
        CancellationToken cancellationToken = default)
    {
        await context.UnderwritingGuidelines.AddAsync(aggregate, cancellationToken);
    }

    public void Update(UnderwritingGuideline aggregate)
    {
        // If the entity is already tracked, let the change tracker detect modifications
        // automatically. Calling DbSet.Update() on a tracked entity with new child entities
        // incorrectly marks them as Modified instead of Added, causing concurrency errors.
        var entry = context.Entry(aggregate);
        if (entry.State == EntityState.Detached)
        {
            context.UnderwritingGuidelines.Update(aggregate);
        }
    }

    public void Remove(UnderwritingGuideline aggregate)
    {
        context.UnderwritingGuidelines.Remove(aggregate);
    }
}
