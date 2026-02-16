namespace Vector.Domain.Submission.Enums;

/// <summary>
/// Status of a clearance check on a submission.
/// </summary>
public enum ClearanceStatus
{
    NotChecked = 0,
    Passed = 1,
    Failed = 2,
    Overridden = 3
}
