using Vector.Domain.Submission.Aggregates;

namespace Vector.Application.Scoring.Services;

/// <summary>
/// Service for calculating winnability scores for submissions.
/// </summary>
public interface IWinnabilityScoringService
{
    /// <summary>
    /// Calculates the winnability score for a submission.
    /// </summary>
    /// <param name="submission">The submission to evaluate.</param>
    /// <returns>The winnability score result.</returns>
    WinnabilityScoreResult CalculateWinnabilityScore(Submission submission);
}

/// <summary>
/// Result of winnability scoring.
/// </summary>
public record WinnabilityScoreResult(
    int OverallScore,
    int CompetitivePositionScore,
    int RelationshipScore,
    int PricingIndicatorScore,
    int TimingScore,
    IReadOnlyList<WinnabilityFactor> Factors,
    IReadOnlyList<string> Recommendations)
{
    /// <summary>
    /// Whether the submission has high winnability (>= 70).
    /// </summary>
    public bool IsHighWinnability => OverallScore >= 70;

    /// <summary>
    /// Whether the submission needs attention (< 50).
    /// </summary>
    public bool NeedsAttention => OverallScore < 50;
}

/// <summary>
/// A factor contributing to the winnability score.
/// </summary>
public record WinnabilityFactor(
    string FactorName,
    string Description,
    int ScoreImpact,
    WinnabilityFactorCategory Category);

/// <summary>
/// Categories of winnability factors.
/// </summary>
public enum WinnabilityFactorCategory
{
    CompetitivePosition,
    Relationship,
    Pricing,
    Timing,
    SubmissionQuality
}
