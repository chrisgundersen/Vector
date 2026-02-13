using Vector.Domain.Routing.Aggregates;
using Vector.Domain.Routing.Enums;

namespace Vector.Domain.Routing;

/// <summary>
/// Repository interface for routing rules.
/// </summary>
public interface IRoutingRuleRepository
{
    Task<RoutingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoutingRule>> GetActiveRulesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoutingRule>> GetByStatusAsync(RoutingRuleStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoutingRule>> GetByStrategyAsync(RoutingStrategy strategy, CancellationToken cancellationToken = default);
    Task AddAsync(RoutingRule rule, CancellationToken cancellationToken = default);
    Task UpdateAsync(RoutingRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
