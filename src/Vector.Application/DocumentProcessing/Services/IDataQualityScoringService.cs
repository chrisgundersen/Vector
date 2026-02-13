using Vector.Domain.DocumentProcessing.Aggregates;
using Vector.Domain.Submission.Aggregates;

namespace Vector.Application.DocumentProcessing.Services;

/// <summary>
/// Service for calculating data quality scores for processed documents and submissions.
/// </summary>
public interface IDataQualityScoringService
{
    /// <summary>
    /// Calculates the data quality score for a processing job.
    /// </summary>
    /// <param name="processingJob">The completed processing job.</param>
    /// <returns>A score breakdown with overall score (0-100).</returns>
    DataQualityScore CalculateJobScore(ProcessingJob processingJob);

    /// <summary>
    /// Calculates the data quality score for a submission.
    /// </summary>
    /// <param name="submission">The submission to score.</param>
    /// <returns>A score breakdown with overall score (0-100).</returns>
    DataQualityScore CalculateSubmissionScore(Submission submission);

    /// <summary>
    /// Calculates the data quality score for a single processed document.
    /// </summary>
    /// <param name="document">The processed document.</param>
    /// <returns>A score breakdown with overall score (0-100).</returns>
    DataQualityScore CalculateDocumentScore(ProcessedDocument document);
}

/// <summary>
/// Breakdown of a data quality score.
/// </summary>
public record DataQualityScore(
    int OverallScore,
    int CompletenessScore,
    int ConfidenceScore,
    int ValidationScore,
    int CoverageScore,
    IReadOnlyList<DataQualityIssue> Issues)
{
    /// <summary>
    /// Creates a default score (all zeros) for cases where scoring isn't applicable.
    /// </summary>
    public static DataQualityScore Default => new(0, 0, 0, 0, 0, []);

    /// <summary>
    /// Whether the overall score indicates high quality (>= 80).
    /// </summary>
    public bool IsHighQuality => OverallScore >= 80;

    /// <summary>
    /// Whether the overall score requires manual review (< 60).
    /// </summary>
    public bool RequiresReview => OverallScore < 60;
}

/// <summary>
/// A specific data quality issue identified during scoring.
/// </summary>
public record DataQualityIssue(
    DataQualityIssueType Type,
    string FieldName,
    string Description,
    DataQualitySeverity Severity);

/// <summary>
/// Types of data quality issues.
/// </summary>
public enum DataQualityIssueType
{
    MissingRequiredField,
    LowConfidence,
    ValidationError,
    MissingDocument,
    InconsistentData,
    FormatError
}

/// <summary>
/// Severity levels for data quality issues.
/// </summary>
public enum DataQualitySeverity
{
    Low,
    Medium,
    High,
    Critical
}
