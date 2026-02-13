namespace Vector.Domain.UnderwritingGuidelines.Enums;

/// <summary>
/// Status of an underwriting guideline.
/// </summary>
public enum GuidelineStatus
{
    /// <summary>
    /// Guideline is in draft mode and not being applied.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Guideline is active and being applied to submissions.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Guideline has been deactivated.
    /// </summary>
    Inactive = 2,

    /// <summary>
    /// Guideline has been archived and is read-only.
    /// </summary>
    Archived = 3
}
