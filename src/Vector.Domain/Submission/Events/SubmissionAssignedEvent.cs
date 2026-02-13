using Vector.Domain.Common;

namespace Vector.Domain.Submission.Events;

/// <summary>
/// Domain event raised when a submission is assigned to an underwriter.
/// </summary>
public sealed record SubmissionAssignedEvent(
    Guid SubmissionId,
    Guid UnderwriterId,
    string UnderwriterName,
    DateTime AssignedAt) : DomainEvent;
