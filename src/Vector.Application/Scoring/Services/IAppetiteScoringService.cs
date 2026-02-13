using Vector.Domain.Submission.Aggregates;
using Vector.Domain.UnderwritingGuidelines.Aggregates;

namespace Vector.Application.Scoring.Services;

/// <summary>
/// Service for calculating appetite scores for submissions.
/// </summary>
public interface IAppetiteScoringService
{
    /// <summary>
    /// Calculates the appetite score for a submission against applicable guidelines.
    /// </summary>
    /// <param name="submission">The submission to evaluate.</param>
    /// <param name="guidelines">The guidelines to evaluate against.</param>
    /// <returns>The appetite score result.</returns>
    AppetiteScoreResult CalculateAppetiteScore(
        Submission submission,
        IEnumerable<UnderwritingGuideline> guidelines);
}

/// <summary>
/// Result of appetite scoring.
/// </summary>
public record AppetiteScoreResult(
    int OverallScore,
    bool IsInAppetite,
    bool RequiresReferral,
    IReadOnlyList<AppetiteScoreFactor> Factors,
    IReadOnlyList<string> DeclineReasons,
    IReadOnlyList<string> ReferralReasons)
{
    /// <summary>
    /// Creates a default "no guidelines" result.
    /// </summary>
    public static AppetiteScoreResult NoGuidelines => new(
        50, // Neutral score
        false,
        true, // Refer when no guidelines
        [],
        [],
        ["No applicable guidelines found"]);
}

/// <summary>
/// A factor contributing to the appetite score.
/// </summary>
public record AppetiteScoreFactor(
    string FactorName,
    string Description,
    int ScoreImpact,
    AppetiteFactorCategory Category);

/// <summary>
/// Categories of appetite factors.
/// </summary>
public enum AppetiteFactorCategory
{
    Industry,
    Geography,
    Size,
    LossHistory,
    Coverage,
    RiskCharacteristics
}
