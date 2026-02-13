using Microsoft.EntityFrameworkCore;
using Vector.Domain.Routing;
using Vector.Domain.Routing.Aggregates;
using Vector.Domain.Routing.Enums;

namespace Vector.Infrastructure.Persistence.Repositories;

public class RoutingDecisionRepository(VectorDbContext context) : IRoutingDecisionRepository
{
    public async Task<RoutingDecision?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.RoutingDecisions
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<RoutingDecision?> GetBySubmissionIdAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        return await context.RoutingDecisions
            .FirstOrDefaultAsync(d => d.SubmissionId == submissionId, cancellationToken);
    }

    public async Task<IReadOnlyList<RoutingDecision>> GetByUnderwriterIdAsync(
        Guid underwriterId,
        CancellationToken cancellationToken = default)
    {
        return await context.RoutingDecisions
            .Where(d => d.AssignedUnderwriterId == underwriterId)
            .OrderByDescending(d => d.DecidedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoutingDecision>> GetByStatusAsync(
        RoutingDecisionStatus status,
        CancellationToken cancellationToken = default)
    {
        return await context.RoutingDecisions
            .Where(d => d.Status == status)
            .OrderByDescending(d => d.DecidedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoutingDecision>> GetPendingAssignmentsAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.RoutingDecisions
            .Where(d => d.Status == RoutingDecisionStatus.Pending)
            .OrderBy(d => d.DecidedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RoutingDecision decision, CancellationToken cancellationToken = default)
    {
        await context.RoutingDecisions.AddAsync(decision, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(RoutingDecision decision, CancellationToken cancellationToken = default)
    {
        context.RoutingDecisions.Update(decision);
        await context.SaveChangesAsync(cancellationToken);
    }
}
