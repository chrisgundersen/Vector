using Vector.Domain.Submission.Entities;

namespace Vector.Domain.Submission.Services;

/// <summary>
/// Service interface for checking submission clearance against existing submissions.
/// </summary>
public interface IClearanceCheckService
{
    /// <summary>
    /// Checks a submission for potential duplicates based on FEIN, insured name, and mailing address.
    /// </summary>
    Task<IReadOnlyList<ClearanceMatch>> CheckAsync(
        Aggregates.Submission submission,
        CancellationToken cancellationToken = default);
}
