using Microsoft.Extensions.Logging;
using Vector.Domain.Routing;
using Vector.Domain.Routing.Aggregates;
using Vector.Domain.Routing.Enums;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Application.Routing.Services;

/// <summary>
/// Service for routing submissions to underwriters based on rules and pairings.
/// </summary>
public class RoutingService(
    IRoutingRuleRepository routingRuleRepository,
    IProducerUnderwriterPairingRepository pairingRepository,
    IRoutingDecisionRepository routingDecisionRepository,
    ILogger<RoutingService> logger) : IRoutingService
{
    public async Task<RoutingResult> RouteSubmissionAsync(
        Submission submission,
        int? appetiteScore = null,
        int? winnabilityScore = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Routing submission {SubmissionNumber}", submission.SubmissionNumber);

        // Build field values for rule evaluation
        var fieldValues = BuildFieldValues(submission);

        // Check existing routing decision
        var existingDecision = await routingDecisionRepository.GetBySubmissionIdAsync(
            submission.Id, cancellationToken);

        if (existingDecision is not null &&
            existingDecision.Status is RoutingDecisionStatus.Accepted)
        {
            logger.LogDebug(
                "Submission {SubmissionNumber} already routed to {Underwriter}",
                submission.SubmissionNumber,
                existingDecision.AssignedUnderwriterName);

            return RoutingResult.Success(
                existingDecision,
                existingDecision.Strategy,
                existingDecision.MatchedRuleName,
                "Submission already assigned");
        }

        // Try routing strategies in order of priority

        // 1. Check producer-underwriter pairings first
        var pairingResult = await TryRoutingByPairingAsync(
            submission, fieldValues, appetiteScore, winnabilityScore, cancellationToken);

        if (pairingResult is not null)
        {
            return pairingResult;
        }

        // 2. Evaluate routing rules
        var ruleResult = await TryRoutingByRulesAsync(
            submission, fieldValues, appetiteScore, winnabilityScore, cancellationToken);

        if (ruleResult is not null)
        {
            return ruleResult;
        }

        // 3. No matching rules or pairings - send to manual queue
        logger.LogInformation(
            "No routing rules matched for submission {SubmissionNumber}, sending to manual queue",
            submission.SubmissionNumber);

        var recommendations = GenerateRoutingRecommendations(submission, appetiteScore, winnabilityScore);

        return RoutingResult.ManualReview(
            "No matching routing rules or producer pairings found",
            recommendations);
    }

    private async Task<RoutingResult?> TryRoutingByPairingAsync(
        Submission submission,
        IReadOnlyDictionary<RuleField, string?> fieldValues,
        int? appetiteScore,
        int? winnabilityScore,
        CancellationToken cancellationToken)
    {
        // If submission has a producer, try to find a pairing
        if (submission.ProducerId is null)
        {
            return null;
        }

        // Get the primary coverage type from the submission
        var primaryCoverage = submission.Coverages.FirstOrDefault();
        if (primaryCoverage is null)
        {
            return null;
        }

        var pairing = await pairingRepository.FindMatchingPairingAsync(
            submission.ProducerId.Value,
            primaryCoverage.Type,
            DateTime.UtcNow,
            cancellationToken);

        if (pairing is null)
        {
            return null;
        }

        logger.LogDebug(
            "Found producer-underwriter pairing for submission {SubmissionNumber}: {Producer} -> {Underwriter}",
            submission.SubmissionNumber,
            pairing.ProducerName,
            pairing.UnderwriterName);

        // Create routing decision
        var decisionResult = RoutingDecision.Create(
            submission.Id,
            submission.SubmissionNumber,
            RoutingStrategy.ProducerPairing);

        if (decisionResult.IsFailure)
        {
            logger.LogWarning(
                "Failed to create routing decision: {Error}",
                decisionResult.Error.Description);
            return null;
        }

        var decision = decisionResult.Value;
        decision.SetMatchedPairing(pairing.Id);
        decision.SetScores(appetiteScore, winnabilityScore);
        decision.SetRoutingReason($"Matched producer pairing: {pairing.ProducerName} -> {pairing.UnderwriterName}");

        var assignResult = decision.AssignToUnderwriter(
            pairing.UnderwriterId,
            pairing.UnderwriterName,
            "Routed via producer-underwriter pairing");

        if (assignResult.IsFailure)
        {
            logger.LogWarning(
                "Failed to assign submission: {Error}",
                assignResult.Error.Description);
            return null;
        }

        await routingDecisionRepository.AddAsync(decision, cancellationToken);

        return RoutingResult.Success(
            decision,
            RoutingStrategy.ProducerPairing,
            null,
            $"Routed via producer pairing to {pairing.UnderwriterName}");
    }

    private async Task<RoutingResult?> TryRoutingByRulesAsync(
        Submission submission,
        IReadOnlyDictionary<RuleField, string?> fieldValues,
        int? appetiteScore,
        int? winnabilityScore,
        CancellationToken cancellationToken)
    {
        var activeRules = await routingRuleRepository.GetActiveRulesAsync(cancellationToken);

        if (activeRules.Count == 0)
        {
            logger.LogDebug("No active routing rules found");
            return null;
        }

        // Evaluate rules in priority order (lower priority number = higher priority)
        var matchingRule = activeRules
            .OrderBy(r => r.Priority)
            .FirstOrDefault(r => r.Matches(fieldValues));

        if (matchingRule is null)
        {
            logger.LogDebug("No routing rules matched submission {SubmissionNumber}", submission.SubmissionNumber);
            return null;
        }

        logger.LogDebug(
            "Routing rule '{RuleName}' matched for submission {SubmissionNumber}",
            matchingRule.Name,
            submission.SubmissionNumber);

        // Create routing decision
        var decisionResult = RoutingDecision.Create(
            submission.Id,
            submission.SubmissionNumber,
            matchingRule.Strategy);

        if (decisionResult.IsFailure)
        {
            logger.LogWarning(
                "Failed to create routing decision: {Error}",
                decisionResult.Error.Description);
            return null;
        }

        var decision = decisionResult.Value;
        decision.SetMatchedRule(matchingRule.Id, matchingRule.Name);
        decision.SetScores(appetiteScore, winnabilityScore);
        decision.SetRoutingReason($"Matched routing rule: {matchingRule.Name}");

        // Assign based on strategy
        switch (matchingRule.Strategy)
        {
            case RoutingStrategy.Direct when matchingRule.TargetUnderwriterId.HasValue:
                var assignResult = decision.AssignToUnderwriter(
                    matchingRule.TargetUnderwriterId.Value,
                    matchingRule.TargetUnderwriterName!,
                    $"Direct routing via rule '{matchingRule.Name}'");

                if (assignResult.IsFailure)
                {
                    return null;
                }
                break;

            case RoutingStrategy.SpecialtyBased when matchingRule.TargetTeamId.HasValue:
                decision.AssignToTeam(
                    matchingRule.TargetTeamId.Value,
                    matchingRule.TargetTeamName!,
                    $"Specialty routing via rule '{matchingRule.Name}'");
                break;

            case RoutingStrategy.ManualQueue:
                // Don't assign, leave in pending for manual assignment
                break;

            default:
                // For round-robin, load-balanced, etc. - would need additional logic
                // For now, send to manual queue
                logger.LogDebug(
                    "Strategy {Strategy} requires additional implementation, sending to manual queue",
                    matchingRule.Strategy);
                break;
        }

        await routingDecisionRepository.AddAsync(decision, cancellationToken);

        return RoutingResult.Success(
            decision,
            matchingRule.Strategy,
            matchingRule.Name,
            $"Routed via rule '{matchingRule.Name}'");
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
            [RuleField.LocationCount] = submission.Locations.Count.ToString()
        };

        // Calculate total insured value
        var tiv = submission.Locations
            .Sum(l => (l.BuildingValue?.Amount ?? 0) + (l.ContentsValue?.Amount ?? 0) + (l.BusinessIncomeValue?.Amount ?? 0));
        values[RuleField.TotalInsuredValue] = tiv.ToString();

        // Add coverage types
        var coverageTypes = string.Join(",", submission.Coverages.Select(c => c.Type.ToString()));
        values[RuleField.CoverageType] = coverageTypes;

        return values;
    }

    private static List<string> GenerateRoutingRecommendations(
        Submission submission,
        int? appetiteScore,
        int? winnabilityScore)
    {
        var recommendations = new List<string>();

        if (appetiteScore.HasValue && appetiteScore < 50)
        {
            recommendations.Add("Low appetite score - consider senior underwriter review");
        }

        if (winnabilityScore.HasValue && winnabilityScore > 80)
        {
            recommendations.Add("High winnability score - prioritize for quick turnaround");
        }

        if (submission.Coverages.Count > 3)
        {
            recommendations.Add("Multi-line submission - consider commercial lines specialist");
        }

        var hasLargeExposure = submission.Locations
            .Any(l => (l.BuildingValue?.Amount ?? 0) > 10_000_000);

        if (hasLargeExposure)
        {
            recommendations.Add("Large property exposure - consider property specialist");
        }

        return recommendations;
    }
}
