using Vector.Domain.Common;
using Vector.Domain.Submission.Enums;

namespace Vector.Domain.Submission.Events;

/// <summary>
/// Domain event raised when a clearance check completes on a submission.
/// </summary>
public sealed record ClearanceCompletedEvent(
    Guid SubmissionId,
    ClearanceStatus Status,
    int MatchCount) : DomainEvent;
