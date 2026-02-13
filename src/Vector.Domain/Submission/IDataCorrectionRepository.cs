using Vector.Domain.Submission.Entities;
using Vector.Domain.Submission.Enums;

namespace Vector.Domain.Submission;

/// <summary>
/// Repository interface for data correction requests.
/// </summary>
public interface IDataCorrectionRepository
{
    Task<DataCorrectionRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataCorrectionRequest>> GetBySubmissionIdAsync(Guid submissionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataCorrectionRequest>> GetByStatusAsync(DataCorrectionStatus status, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataCorrectionRequest>> GetPendingBySubmissionIdAsync(Guid submissionId, CancellationToken cancellationToken = default);
    Task AddAsync(DataCorrectionRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(DataCorrectionRequest request, CancellationToken cancellationToken = default);
}
