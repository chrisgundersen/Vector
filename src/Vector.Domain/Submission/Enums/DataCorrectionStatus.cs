namespace Vector.Domain.Submission.Enums;

/// <summary>
/// Status of a data correction request.
/// </summary>
public enum DataCorrectionStatus
{
    Pending = 0,
    UnderReview = 1,
    Approved = 2,
    Rejected = 3,
    Applied = 4
}
