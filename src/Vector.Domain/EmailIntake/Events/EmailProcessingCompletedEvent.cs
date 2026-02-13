using Vector.Domain.Common;

namespace Vector.Domain.EmailIntake.Events;

/// <summary>
/// Domain event raised when all attachments from an email have been processed.
/// </summary>
public sealed record EmailProcessingCompletedEvent(
    Guid InboundEmailId,
    int TotalAttachments,
    int SuccessfulExtractions,
    int FailedExtractions) : DomainEvent;
