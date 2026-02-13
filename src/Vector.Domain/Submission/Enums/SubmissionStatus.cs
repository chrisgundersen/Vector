namespace Vector.Domain.Submission.Enums;

/// <summary>
/// Status of a submission in the underwriting workflow.
/// </summary>
public enum SubmissionStatus
{
    Draft = 0,
    Received = 1,
    InReview = 2,
    PendingInformation = 3,
    Quoted = 4,
    Declined = 5,
    Bound = 6,
    Withdrawn = 7,
    Expired = 8
}
