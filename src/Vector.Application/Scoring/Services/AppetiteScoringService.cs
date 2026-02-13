using Microsoft.Extensions.Logging;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Entities;
using Vector.Domain.UnderwritingGuidelines.Aggregates;
using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Application.Scoring.Services;

/// <summary>
/// Service for calculating appetite scores for submissions.
/// </summary>
public class AppetiteScoringService(
    ILogger<AppetiteScoringService> logger) : IAppetiteScoringService
{
    public AppetiteScoreResult CalculateAppetiteScore(
        Submission submission,
        IEnumerable<UnderwritingGuideline> guidelines)
    {
        var guidelineList = guidelines.ToList();

        if (guidelineList.Count == 0)
        {
            logger.LogDebug(
                "No guidelines provided for appetite scoring of submission {SubmissionNumber}",
                submission.SubmissionNumber);
            return AppetiteScoreResult.NoGuidelines;
        }

        var factors = new List<AppetiteScoreFactor>();
        var declineReasons = new List<string>();
        var referralReasons = new List<string>();

        // Build field values from submission
        var fieldValues = BuildFieldValues(submission);

        // Evaluate against all guidelines
        var allResults = new List<RuleEvaluationResult>();
        foreach (var guideline in guidelineList)
        {
            var results = guideline.Evaluate(fieldValues);
            allResults.AddRange(results);
        }

        // Process rule results
        var baseScore = 70; // Start with a neutral-positive score
        var hasDecline = false;
        var hasReferral = false;

        foreach (var result in allResults)
        {
            switch (result.Action)
            {
                case RuleAction.Decline:
                    hasDecline = true;
                    declineReasons.Add(result.Message ?? $"Rule '{result.RuleName}' triggered decline");
                    factors.Add(new AppetiteScoreFactor(
                        result.RuleName,
                        result.Message ?? "Automatic decline",
                        -50,
                        DetermineCategory(result.RuleName)));
                    break;

                case RuleAction.Refer:
                    hasReferral = true;
                    referralReasons.Add(result.Message ?? $"Rule '{result.RuleName}' requires referral");
                    factors.Add(new AppetiteScoreFactor(
                        result.RuleName,
                        result.Message ?? "Requires manual review",
                        -10,
                        DetermineCategory(result.RuleName)));
                    break;

                case RuleAction.Accept:
                    factors.Add(new AppetiteScoreFactor(
                        result.RuleName,
                        result.Message ?? "Within appetite",
                        10,
                        DetermineCategory(result.RuleName)));
                    break;

                case RuleAction.AdjustScore:
                    if (result.ScoreAdjustment.HasValue)
                    {
                        factors.Add(new AppetiteScoreFactor(
                            result.RuleName,
                            result.Message ?? $"Score adjustment: {result.ScoreAdjustment:+#;-#;0}",
                            result.ScoreAdjustment.Value,
                            DetermineCategory(result.RuleName)));
                    }
                    break;

                case RuleAction.RequireInformation:
                    referralReasons.Add(result.Message ?? "Additional information required");
                    break;
            }
        }

        // Calculate final score
        var totalAdjustment = factors.Sum(f => f.ScoreImpact);
        var finalScore = Math.Clamp(baseScore + totalAdjustment, 0, 100);

        // Determine appetite
        var isInAppetite = !hasDecline && finalScore >= 50;
        var requiresReferral = hasReferral || (finalScore >= 40 && finalScore < 60);

        logger.LogDebug(
            "Calculated appetite score for {SubmissionNumber}: Score={Score}, InAppetite={InAppetite}, " +
            "Referral={Referral}, Factors={FactorCount}",
            submission.SubmissionNumber,
            finalScore,
            isInAppetite,
            requiresReferral,
            factors.Count);

        return new AppetiteScoreResult(
            finalScore,
            isInAppetite,
            requiresReferral,
            factors,
            declineReasons,
            referralReasons);
    }

    private static Dictionary<RuleField, string?> BuildFieldValues(Submission submission)
    {
        var values = new Dictionary<RuleField, string?>
        {
            [RuleField.InsuredName] = submission.Insured.Name,
            [RuleField.InsuredState] = submission.Insured.MailingAddress?.State,
            [RuleField.InsuredCity] = submission.Insured.MailingAddress?.City,
            [RuleField.InsuredZipCode] = submission.Insured.MailingAddress?.PostalCode,
            [RuleField.YearsInBusiness] = submission.Insured.YearsInBusiness?.ToString(),
            [RuleField.EmployeeCount] = submission.Insured.EmployeeCount?.ToString(),
            [RuleField.AnnualRevenue] = submission.Insured.AnnualRevenue?.Amount.ToString(),
            [RuleField.NAICSCode] = submission.Insured.Industry?.NaicsCode,
            [RuleField.SICCode] = submission.Insured.Industry?.SicCode,
            [RuleField.BusinessDescription] = submission.Insured.Industry?.Description,
            [RuleField.TotalLossCount] = submission.LossHistory.Count.ToString(),
            [RuleField.OpenClaimsCount] = submission.LossHistory.Count(l =>
                l.Status == LossStatus.Open ||
                l.Status == LossStatus.Reopened).ToString(),
            [RuleField.LocationCount] = submission.Locations.Count.ToString()
        };

        // Calculate total incurred
        var totalIncurred = submission.LossHistory
            .Where(l => l.IncurredAmount is not null)
            .Sum(l => l.IncurredAmount!.Amount);
        values[RuleField.TotalIncurredAmount] = totalIncurred.ToString();

        // Calculate largest claim
        var largestClaim = submission.LossHistory.Count > 0
            ? submission.LossHistory.Max(l => l.IncurredAmount?.Amount ?? l.PaidAmount?.Amount ?? 0)
            : 0;
        values[RuleField.LargestClaimAmount] = largestClaim.ToString();

        // Calculate total insured value
        var tiv = submission.Locations
            .Sum(l => (l.BuildingValue?.Amount ?? 0) + (l.ContentsValue?.Amount ?? 0) + (l.BusinessIncomeValue?.Amount ?? 0));
        values[RuleField.TotalInsuredValue] = tiv.ToString();

        // Add coverage types
        var coverageTypes = string.Join(",", submission.Coverages.Select(c => c.Type.ToString()));
        values[RuleField.CoverageType] = coverageTypes;

        return values;
    }

    private static AppetiteFactorCategory DetermineCategory(string ruleName)
    {
        var nameLower = ruleName.ToLowerInvariant();

        if (nameLower.Contains("industry") || nameLower.Contains("naics") || nameLower.Contains("sic"))
            return AppetiteFactorCategory.Industry;

        if (nameLower.Contains("state") || nameLower.Contains("geography") || nameLower.Contains("region"))
            return AppetiteFactorCategory.Geography;

        if (nameLower.Contains("revenue") || nameLower.Contains("employee") || nameLower.Contains("size"))
            return AppetiteFactorCategory.Size;

        if (nameLower.Contains("loss") || nameLower.Contains("claim"))
            return AppetiteFactorCategory.LossHistory;

        if (nameLower.Contains("coverage") || nameLower.Contains("limit"))
            return AppetiteFactorCategory.Coverage;

        return AppetiteFactorCategory.RiskCharacteristics;
    }
}
