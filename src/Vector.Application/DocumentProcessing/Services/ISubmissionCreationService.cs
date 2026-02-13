using Vector.Domain.Common;
using Vector.Domain.DocumentProcessing.Aggregates;
using Vector.Domain.Submission.Aggregates;

namespace Vector.Application.DocumentProcessing.Services;

/// <summary>
/// Service for creating Submission aggregates from processed document data.
/// </summary>
public interface ISubmissionCreationService
{
    /// <summary>
    /// Creates a Submission from a completed processing job.
    /// </summary>
    /// <param name="processingJob">The completed processing job with extracted documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created Submission or an error.</returns>
    Task<Result<Submission>> CreateSubmissionFromJobAsync(
        ProcessingJob processingJob,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing Submission with data from a processing job.
    /// </summary>
    /// <param name="submission">The submission to update.</param>
    /// <param name="processingJob">The processing job with extracted data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> EnrichSubmissionAsync(
        Submission submission,
        ProcessingJob processingJob,
        CancellationToken cancellationToken = default);
}
