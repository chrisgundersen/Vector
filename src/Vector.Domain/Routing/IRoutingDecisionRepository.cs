using Vector.Domain.Routing.Aggregates;
using Vector.Domain.Routing.Enums;

namespace Vector.Domain.Routing;

/// <summary>
/// Repository interface for routing decisions.
/// </summary>
public interface IRoutingDecisionRepository
{
    Task<RoutingDecision?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RoutingDecision?> GetBySubmissionIdAsync(Guid submissionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoutingDecision>> GetByUnderwriterIdAsync(Guid underwriterId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoutingDecision>> GetByStatusAsync(RoutingDecisionStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoutingDecision>> GetPendingAssignmentsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(RoutingDecision decision, CancellationToken cancellationToken = default);
    Task UpdateAsync(RoutingDecision decision, CancellationToken cancellationToken = default);
}
