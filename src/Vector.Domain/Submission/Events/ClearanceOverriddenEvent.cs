using Vector.Domain.Common;

namespace Vector.Domain.Submission.Events;

/// <summary>
/// Domain event raised when a failed clearance is overridden by an underwriter.
/// </summary>
public sealed record ClearanceOverriddenEvent(
    Guid SubmissionId,
    Guid OverriddenByUserId,
    string Reason) : DomainEvent;
