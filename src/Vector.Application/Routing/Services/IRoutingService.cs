using Vector.Domain.Routing.Aggregates;
using Vector.Domain.Routing.Enums;
using Vector.Domain.Submission.Aggregates;

namespace Vector.Application.Routing.Services;

/// <summary>
/// Service for routing submissions to underwriters.
/// </summary>
public interface IRoutingService
{
    /// <summary>
    /// Determines the routing for a submission based on rules and pairings.
    /// </summary>
    /// <param name="submission">The submission to route.</param>
    /// <param name="appetiteScore">Optional appetite score for prioritization.</param>
    /// <param name="winnabilityScore">Optional winnability score for prioritization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The routing result with assignment details.</returns>
    Task<RoutingResult> RouteSubmissionAsync(
        Submission submission,
        int? appetiteScore = null,
        int? winnabilityScore = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of routing a submission.
/// </summary>
public record RoutingResult(
    bool IsRouted,
    RoutingDecision? Decision,
    RoutingStrategy Strategy,
    string? MatchedRuleName,
    string Reason,
    IReadOnlyList<string> Recommendations)
{
    public static RoutingResult Success(
        RoutingDecision decision,
        RoutingStrategy strategy,
        string? matchedRuleName,
        string reason) =>
        new(true, decision, strategy, matchedRuleName, reason, []);

    public static RoutingResult ManualReview(string reason, IReadOnlyList<string>? recommendations = null) =>
        new(false, null, RoutingStrategy.ManualQueue, null, reason, recommendations ?? []);
}
