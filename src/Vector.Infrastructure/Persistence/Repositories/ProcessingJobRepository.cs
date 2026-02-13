using Microsoft.EntityFrameworkCore;
using Vector.Domain.Common;
using Vector.Domain.DocumentProcessing;
using Vector.Domain.DocumentProcessing.Aggregates;
using Vector.Domain.DocumentProcessing.Enums;

namespace Vector.Infrastructure.Persistence.Repositories;

public class ProcessingJobRepository(VectorDbContext context) : IProcessingJobRepository
{
    public IUnitOfWork UnitOfWork => context;

    public async Task<ProcessingJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.ProcessingJobs
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task AddAsync(ProcessingJob aggregate, CancellationToken cancellationToken = default)
    {
        await context.ProcessingJobs.AddAsync(aggregate, cancellationToken);
    }

    public void Update(ProcessingJob aggregate)
    {
        context.ProcessingJobs.Update(aggregate);
    }

    public void Remove(ProcessingJob aggregate)
    {
        context.ProcessingJobs.Remove(aggregate);
    }

    public async Task<ProcessingJob?> GetByInboundEmailIdAsync(
        Guid inboundEmailId,
        CancellationToken cancellationToken = default)
    {
        return await context.ProcessingJobs
            .FirstOrDefaultAsync(j => j.InboundEmailId == inboundEmailId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProcessingJob>> GetByStatusAsync(
        ProcessingStatus status,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await context.ProcessingJobs
            .Where(j => j.Status == status)
            .OrderBy(j => j.StartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProcessingJob>> GetActiveJobsForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await context.ProcessingJobs
            .IgnoreQueryFilters()
            .Where(j =>
                j.TenantId == tenantId &&
                (j.Status == ProcessingStatus.Pending ||
                 j.Status == ProcessingStatus.Classifying ||
                 j.Status == ProcessingStatus.Extracting ||
                 j.Status == ProcessingStatus.Validating))
            .OrderBy(j => j.StartedAt)
            .ToListAsync(cancellationToken);
    }
}
