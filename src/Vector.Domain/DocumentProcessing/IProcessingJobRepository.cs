using Vector.Domain.Common;
using Vector.Domain.DocumentProcessing.Aggregates;
using Vector.Domain.DocumentProcessing.Enums;

namespace Vector.Domain.DocumentProcessing;

/// <summary>
/// Repository interface for ProcessingJob aggregate.
/// </summary>
public interface IProcessingJobRepository : IRepository<ProcessingJob>
{
    /// <summary>
    /// Gets a processing job by the inbound email ID.
    /// </summary>
    Task<ProcessingJob?> GetByInboundEmailIdAsync(Guid inboundEmailId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets processing jobs with a specific status.
    /// </summary>
    Task<IReadOnlyList<ProcessingJob>> GetByStatusAsync(ProcessingStatus status, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets processing jobs that are pending or in progress for a tenant.
    /// </summary>
    Task<IReadOnlyList<ProcessingJob>> GetActiveJobsForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
