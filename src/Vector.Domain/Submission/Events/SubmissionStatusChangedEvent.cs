using Vector.Domain.Common;
using Vector.Domain.Submission.Enums;

namespace Vector.Domain.Submission.Events;

/// <summary>
/// Domain event raised when a submission's status changes.
/// </summary>
public sealed record SubmissionStatusChangedEvent(
    Guid SubmissionId,
    SubmissionStatus PreviousStatus,
    SubmissionStatus NewStatus,
    string? Reason) : DomainEvent;
