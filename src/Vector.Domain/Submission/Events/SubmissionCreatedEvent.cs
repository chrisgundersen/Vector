using Vector.Domain.Common;

namespace Vector.Domain.Submission.Events;

/// <summary>
/// Domain event raised when a new submission is created.
/// </summary>
public sealed record SubmissionCreatedEvent(
    Guid SubmissionId,
    Guid TenantId,
    Guid? ProcessingJobId,
    string InsuredName,
    DateTime ReceivedAt) : DomainEvent;
