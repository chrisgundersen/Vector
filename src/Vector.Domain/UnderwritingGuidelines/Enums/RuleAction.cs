namespace Vector.Domain.UnderwritingGuidelines.Enums;

/// <summary>
/// Action to take when a rule matches.
/// </summary>
public enum RuleAction
{
    /// <summary>
    /// Accept the submission for this criteria.
    /// </summary>
    Accept = 0,

    /// <summary>
    /// Decline the submission.
    /// </summary>
    Decline = 1,

    /// <summary>
    /// Refer to underwriter for manual review.
    /// </summary>
    Refer = 2,

    /// <summary>
    /// Apply a score adjustment.
    /// </summary>
    AdjustScore = 3,

    /// <summary>
    /// Flag for additional information required.
    /// </summary>
    RequireInformation = 4,

    /// <summary>
    /// Apply a pricing modifier.
    /// </summary>
    ApplyModifier = 5
}
