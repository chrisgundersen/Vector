using Vector.Domain.Common;

namespace Vector.Domain.EmailIntake.Events;

/// <summary>
/// Domain event raised when a new email is received and stored.
/// </summary>
public sealed record EmailReceivedEvent(
    Guid InboundEmailId,
    string Subject,
    string FromAddress,
    string MailboxId,
    int AttachmentCount) : DomainEvent;
