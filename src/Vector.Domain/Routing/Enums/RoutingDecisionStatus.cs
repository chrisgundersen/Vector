namespace Vector.Domain.Routing.Enums;

/// <summary>
/// Status of a routing decision.
/// </summary>
public enum RoutingDecisionStatus
{
    /// <summary>
    /// Decision is pending assignment.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Submission has been assigned to an underwriter.
    /// </summary>
    Assigned = 1,

    /// <summary>
    /// Underwriter accepted the assignment.
    /// </summary>
    Accepted = 2,

    /// <summary>
    /// Underwriter declined the assignment.
    /// </summary>
    Declined = 3,

    /// <summary>
    /// Submission was reassigned to another underwriter.
    /// </summary>
    Reassigned = 4,

    /// <summary>
    /// Assignment was escalated to a manager.
    /// </summary>
    Escalated = 5
}
