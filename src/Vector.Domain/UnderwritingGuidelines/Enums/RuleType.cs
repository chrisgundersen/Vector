namespace Vector.Domain.UnderwritingGuidelines.Enums;

/// <summary>
/// Type of underwriting rule.
/// </summary>
public enum RuleType
{
    /// <summary>
    /// Rule determines appetite for a submission.
    /// </summary>
    Appetite = 0,

    /// <summary>
    /// Rule determines eligibility requirements.
    /// </summary>
    Eligibility = 1,

    /// <summary>
    /// Rule for data validation.
    /// </summary>
    Validation = 2,

    /// <summary>
    /// Rule for pricing adjustments.
    /// </summary>
    Pricing = 3,

    /// <summary>
    /// Rule for automatic decline.
    /// </summary>
    Decline = 4
}
