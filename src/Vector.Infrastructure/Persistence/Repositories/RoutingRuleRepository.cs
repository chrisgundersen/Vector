using Microsoft.EntityFrameworkCore;
using Vector.Domain.Routing;
using Vector.Domain.Routing.Aggregates;
using Vector.Domain.Routing.Enums;

namespace Vector.Infrastructure.Persistence.Repositories;

public class RoutingRuleRepository(VectorDbContext context) : IRoutingRuleRepository
{
    public async Task<RoutingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.RoutingRules
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<RoutingRule>> GetActiveRulesAsync(CancellationToken cancellationToken = default)
    {
        return await context.RoutingRules
            .Where(r => r.Status == RoutingRuleStatus.Active)
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoutingRule>> GetByStatusAsync(
        RoutingRuleStatus status,
        CancellationToken cancellationToken = default)
    {
        return await context.RoutingRules
            .Where(r => r.Status == status)
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoutingRule>> GetByStrategyAsync(
        RoutingStrategy strategy,
        CancellationToken cancellationToken = default)
    {
        return await context.RoutingRules
            .Where(r => r.Strategy == strategy)
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RoutingRule rule, CancellationToken cancellationToken = default)
    {
        await context.RoutingRules.AddAsync(rule, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(RoutingRule rule, CancellationToken cancellationToken = default)
    {
        context.RoutingRules.Update(rule);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await GetByIdAsync(id, cancellationToken);
        if (rule is not null)
        {
            context.RoutingRules.Remove(rule);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
