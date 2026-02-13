using Microsoft.Extensions.Logging;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Entities;

namespace Vector.Application.Scoring.Services;

/// <summary>
/// Service for calculating winnability scores for submissions.
/// </summary>
public class WinnabilityScoringService(
    ILogger<WinnabilityScoringService> logger) : IWinnabilityScoringService
{
    public WinnabilityScoreResult CalculateWinnabilityScore(Submission submission)
    {
        var factors = new List<WinnabilityFactor>();
        var recommendations = new List<string>();

        // Calculate component scores
        var competitiveScore = CalculateCompetitivePositionScore(submission, factors, recommendations);
        var relationshipScore = CalculateRelationshipScore(submission, factors, recommendations);
        var pricingScore = CalculatePricingIndicatorScore(submission, factors, recommendations);
        var timingScore = CalculateTimingScore(submission, factors, recommendations);

        // Calculate overall score (weighted average)
        // Competitive: 30%, Relationship: 25%, Pricing: 25%, Timing: 20%
        var overallScore = (int)(
            competitiveScore * 0.30 +
            relationshipScore * 0.25 +
            pricingScore * 0.25 +
            timingScore * 0.20);

        // Add general recommendations based on score
        if (overallScore < 50)
        {
            recommendations.Add("Consider accelerating response time to improve competitiveness");
        }

        if (competitiveScore < 60)
        {
            recommendations.Add("Review pricing strategy against market conditions");
        }

        logger.LogDebug(
            "Calculated winnability score for {SubmissionNumber}: Overall={Overall}, " +
            "Competitive={Competitive}, Relationship={Relationship}, Pricing={Pricing}, Timing={Timing}",
            submission.SubmissionNumber,
            overallScore,
            competitiveScore,
            relationshipScore,
            pricingScore,
            timingScore);

        return new WinnabilityScoreResult(
            overallScore,
            competitiveScore,
            relationshipScore,
            pricingScore,
            timingScore,
            factors,
            recommendations);
    }

    private int CalculateCompetitivePositionScore(
        Submission submission,
        List<WinnabilityFactor> factors,
        List<string> recommendations)
    {
        var score = 70; // Base score

        // Check if submission has multiple coverages (bundling advantage)
        if (submission.Coverages.Count >= 2)
        {
            score += 10;
            factors.Add(new WinnabilityFactor(
                "Multi-Line Package",
                "Submission includes multiple coverage types",
                10,
                WinnabilityFactorCategory.CompetitivePosition));
        }

        // Check for complete data (better pricing ability)
        var hasCompleteInsuredInfo = !string.IsNullOrWhiteSpace(submission.Insured.Name) &&
                                     submission.Insured.MailingAddress is not null &&
                                     submission.Insured.Industry is not null;

        if (hasCompleteInsuredInfo)
        {
            score += 5;
            factors.Add(new WinnabilityFactor(
                "Complete Information",
                "Insured information is complete for accurate pricing",
                5,
                WinnabilityFactorCategory.CompetitivePosition));
        }
        else
        {
            score -= 10;
            factors.Add(new WinnabilityFactor(
                "Incomplete Information",
                "Missing insured data may affect pricing accuracy",
                -10,
                WinnabilityFactorCategory.CompetitivePosition));
            recommendations.Add("Request complete insured information for accurate quoting");
        }

        // Favorable loss history
        if (submission.LossHistory.Count == 0 || !submission.HasOpenClaims)
        {
            score += 10;
            factors.Add(new WinnabilityFactor(
                "Clean Loss History",
                "No open claims, favorable loss experience",
                10,
                WinnabilityFactorCategory.CompetitivePosition));
        }
        else
        {
            var openClaims = submission.LossHistory.Count(l =>
                l.Status == LossStatus.Open || l.Status == LossStatus.Reopened);
            score -= openClaims * 5;
            factors.Add(new WinnabilityFactor(
                "Open Claims",
                $"{openClaims} open claim(s) may affect competitiveness",
                -openClaims * 5,
                WinnabilityFactorCategory.CompetitivePosition));
        }

        return Math.Clamp(score, 0, 100);
    }

    private int CalculateRelationshipScore(
        Submission submission,
        List<WinnabilityFactor> factors,
        List<string> recommendations)
    {
        var score = 50; // Base score (neutral - no relationship data yet)

        // Check for renewal (existing relationship)
        // This would typically come from a producer/customer relationship database
        // For now, use insured info completeness as a proxy

        if (submission.Insured.YearsInBusiness.HasValue && submission.Insured.YearsInBusiness > 5)
        {
            score += 15;
            factors.Add(new WinnabilityFactor(
                "Established Business",
                "Insured has been in business over 5 years",
                15,
                WinnabilityFactorCategory.Relationship));
        }

        if (submission.Insured.AnnualRevenue is not null)
        {
            // Larger accounts often have stronger relationships
            if (submission.Insured.AnnualRevenue.Amount >= 10_000_000m)
            {
                score += 10;
                factors.Add(new WinnabilityFactor(
                    "Large Account",
                    "Annual revenue over $10M indicates potential for strong relationship",
                    10,
                    WinnabilityFactorCategory.Relationship));
            }
        }

        // Source of submission affects relationship potential
        // If from existing producer, typically higher relationship score
        // (This would need producer relationship data)

        return Math.Clamp(score, 0, 100);
    }

    private int CalculatePricingIndicatorScore(
        Submission submission,
        List<WinnabilityFactor> factors,
        List<string> recommendations)
    {
        var score = 60; // Base score

        // Check for reasonable coverage limits
        foreach (var coverage in submission.Coverages)
        {
            if (coverage.RequestedLimit is not null)
            {
                score += 5;
            }
            else
            {
                score -= 5;
                recommendations.Add($"Request specific limit for {coverage.Type} coverage");
            }

            if (coverage.RequestedDeductible is not null)
            {
                score += 3;
            }
        }

        // Good data means better pricing
        if (submission.Locations.Count > 0)
        {
            var locationsWithValues = submission.Locations.Count(l => l.BuildingValue is not null);
            if (locationsWithValues == submission.Locations.Count)
            {
                score += 10;
                factors.Add(new WinnabilityFactor(
                    "Complete SOV",
                    "All locations have building values",
                    10,
                    WinnabilityFactorCategory.Pricing));
            }
        }

        // Loss ratio indicators
        if (submission.LossHistory.Count > 0)
        {
            var totalIncurred = submission.LossHistory
                .Where(l => l.IncurredAmount is not null)
                .Sum(l => l.IncurredAmount!.Amount);

            // Compare to revenue if available
            if (submission.Insured.AnnualRevenue is not null &&
                submission.Insured.AnnualRevenue.Amount > 0)
            {
                var lossRatio = totalIncurred / submission.Insured.AnnualRevenue.Amount;
                if (lossRatio < 0.01m) // Less than 1% loss ratio
                {
                    score += 15;
                    factors.Add(new WinnabilityFactor(
                        "Low Loss Ratio",
                        "Historical loss ratio under 1%",
                        15,
                        WinnabilityFactorCategory.Pricing));
                }
                else if (lossRatio > 0.05m) // More than 5%
                {
                    score -= 10;
                    factors.Add(new WinnabilityFactor(
                        "High Loss Ratio",
                        "Historical loss ratio over 5%",
                        -10,
                        WinnabilityFactorCategory.Pricing));
                }
            }
        }

        return Math.Clamp(score, 0, 100);
    }

    private int CalculateTimingScore(
        Submission submission,
        List<WinnabilityFactor> factors,
        List<string> recommendations)
    {
        var score = 70; // Base score

        // Check effective date timing
        if (submission.EffectiveDate.HasValue)
        {
            var daysUntilEffective = (submission.EffectiveDate.Value - DateTime.UtcNow).TotalDays;

            if (daysUntilEffective < 7)
            {
                score -= 20;
                factors.Add(new WinnabilityFactor(
                    "Rush Submission",
                    "Less than 7 days to effective date",
                    -20,
                    WinnabilityFactorCategory.Timing));
                recommendations.Add("Prioritize this submission due to tight timing");
            }
            else if (daysUntilEffective is >= 7 and < 14)
            {
                score -= 5;
                factors.Add(new WinnabilityFactor(
                    "Short Lead Time",
                    "1-2 weeks to effective date",
                    -5,
                    WinnabilityFactorCategory.Timing));
            }
            else if (daysUntilEffective is >= 30 and < 60)
            {
                score += 10;
                factors.Add(new WinnabilityFactor(
                    "Good Lead Time",
                    "30-60 days to effective date allows thorough review",
                    10,
                    WinnabilityFactorCategory.Timing));
            }
        }
        else
        {
            score -= 10;
            recommendations.Add("Confirm effective date with producer");
        }

        // Submission age (if received date available)
        var submissionAge = (DateTime.UtcNow - submission.ReceivedAt).TotalDays;
        if (submissionAge > 7)
        {
            score -= 10;
            factors.Add(new WinnabilityFactor(
                "Aging Submission",
                $"Submission is {submissionAge:F0} days old",
                -10,
                WinnabilityFactorCategory.Timing));
            recommendations.Add("Respond quickly - submission has been in queue over a week");
        }

        return Math.Clamp(score, 0, 100);
    }
}
