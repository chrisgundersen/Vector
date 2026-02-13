using Vector.Domain.Common;
using Vector.Domain.EmailIntake.ValueObjects;

namespace Vector.Domain.EmailIntake.Aggregates;

/// <summary>
/// Entity representing an attachment extracted from an inbound email.
/// </summary>
public sealed class EmailAttachment : Entity
{
    public AttachmentMetadata Metadata { get; private set; } = null!;
    public string BlobStorageUrl { get; private set; } = string.Empty;
    public DateTime ExtractedAt { get; private set; }
    public EmailAttachmentStatus Status { get; private set; }
    public string? FailureReason { get; private set; }

    private EmailAttachment()
    {
    }

    internal EmailAttachment(
        Guid id,
        AttachmentMetadata metadata,
        string blobStorageUrl) : base(id)
    {
        Metadata = metadata;
        BlobStorageUrl = blobStorageUrl;
        ExtractedAt = DateTime.UtcNow;
        Status = EmailAttachmentStatus.Extracted;
    }

    public void MarkAsProcessed()
    {
        Status = EmailAttachmentStatus.Processed;
    }

    public void MarkAsFailed(string reason)
    {
        Status = EmailAttachmentStatus.Failed;
        FailureReason = reason;
    }
}

public enum EmailAttachmentStatus
{
    Extracted = 0,
    Processed = 1,
    Failed = 2
}
