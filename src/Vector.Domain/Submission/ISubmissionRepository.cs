using Vector.Domain.Common;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Enums;

namespace Vector.Domain.Submission;

/// <summary>
/// Repository interface for Submission aggregate.
/// </summary>
public interface ISubmissionRepository : IRepository<Aggregates.Submission>
{
    /// <summary>
    /// Gets a submission by its submission number.
    /// </summary>
    Task<Aggregates.Submission?> GetBySubmissionNumberAsync(Guid tenantId, string submissionNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a submission by the processing job ID.
    /// </summary>
    Task<Aggregates.Submission?> GetByProcessingJobIdAsync(Guid processingJobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets submissions by status for a tenant.
    /// </summary>
    Task<IReadOnlyList<Aggregates.Submission>> GetByStatusAsync(Guid tenantId, SubmissionStatus status, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets submissions assigned to a specific underwriter.
    /// </summary>
    Task<IReadOnlyList<Aggregates.Submission>> GetByUnderwriterAsync(Guid underwriterId, SubmissionStatus? status, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets submissions for a specific producer.
    /// </summary>
    Task<IReadOnlyList<Aggregates.Submission>> GetByProducerAsync(Guid producerId, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the next submission number for a tenant.
    /// </summary>
    Task<string> GenerateSubmissionNumberAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
