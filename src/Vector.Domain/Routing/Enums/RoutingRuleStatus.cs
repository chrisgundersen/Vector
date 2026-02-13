namespace Vector.Domain.Routing.Enums;

/// <summary>
/// Status of a routing rule.
/// </summary>
public enum RoutingRuleStatus
{
    /// <summary>
    /// Rule is in draft and not yet active.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Rule is active and being used for routing decisions.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Rule is temporarily disabled.
    /// </summary>
    Inactive = 2,

    /// <summary>
    /// Rule has been archived and is no longer used.
    /// </summary>
    Archived = 3
}
