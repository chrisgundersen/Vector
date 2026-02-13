using Vector.Domain.Common;

namespace Vector.Domain.EmailIntake.Events;

/// <summary>
/// Domain event raised when an attachment is extracted from an email.
/// </summary>
public sealed record AttachmentExtractedEvent(
    Guid EmailAttachmentId,
    Guid InboundEmailId,
    string FileName,
    string ContentType,
    long SizeInBytes,
    string BlobStorageUrl) : DomainEvent;
