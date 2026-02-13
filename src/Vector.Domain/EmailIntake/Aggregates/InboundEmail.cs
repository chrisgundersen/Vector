using Vector.Domain.Common;
using Vector.Domain.EmailIntake.Events;
using Vector.Domain.EmailIntake.ValueObjects;

namespace Vector.Domain.EmailIntake.Aggregates;

/// <summary>
/// Aggregate root representing an email received for submission processing.
/// </summary>
public sealed class InboundEmail : AuditableAggregateRoot, IMultiTenantEntity
{
    private readonly List<EmailAttachment> _attachments = [];

    public Guid TenantId { get; private set; }
    public string ExternalMessageId { get; private set; } = string.Empty;
    public string MailboxId { get; private set; } = string.Empty;
    public EmailAddress FromAddress { get; private set; } = null!;
    public string Subject { get; private set; } = string.Empty;
    public string BodyPreview { get; private set; } = string.Empty;
    public ContentHash ContentHash { get; private set; } = null!;
    public DateTime ReceivedAt { get; private set; }
    public InboundEmailStatus Status { get; private set; }
    public string? ProcessingError { get; private set; }

    public IReadOnlyCollection<EmailAttachment> Attachments => _attachments.AsReadOnly();

    private InboundEmail()
    {
    }

    private InboundEmail(
        Guid id,
        Guid tenantId,
        string externalMessageId,
        string mailboxId,
        EmailAddress fromAddress,
        string subject,
        string bodyPreview,
        ContentHash contentHash,
        DateTime receivedAt) : base(id)
    {
        TenantId = tenantId;
        ExternalMessageId = externalMessageId;
        MailboxId = mailboxId;
        FromAddress = fromAddress;
        Subject = subject;
        BodyPreview = bodyPreview;
        ContentHash = contentHash;
        ReceivedAt = receivedAt;
        Status = InboundEmailStatus.Received;
    }

    public static Result<InboundEmail> Create(
        Guid tenantId,
        string externalMessageId,
        string mailboxId,
        string fromAddress,
        string subject,
        string bodyPreview,
        string emailContent,
        DateTime receivedAt)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<InboundEmail>(InboundEmailErrors.InvalidTenant);
        }

        if (string.IsNullOrWhiteSpace(externalMessageId))
        {
            return Result.Failure<InboundEmail>(InboundEmailErrors.ExternalMessageIdRequired);
        }

        if (string.IsNullOrWhiteSpace(mailboxId))
        {
            return Result.Failure<InboundEmail>(InboundEmailErrors.MailboxIdRequired);
        }

        var fromAddressResult = EmailAddress.Create(fromAddress);
        if (fromAddressResult.IsFailure)
        {
            return Result.Failure<InboundEmail>(fromAddressResult.Error);
        }

        var contentHash = ContentHash.ComputeSha256(emailContent);

        var email = new InboundEmail(
            Guid.NewGuid(),
            tenantId,
            externalMessageId,
            mailboxId,
            fromAddressResult.Value,
            subject ?? string.Empty,
            bodyPreview ?? string.Empty,
            contentHash,
            receivedAt);

        return Result.Success(email);
    }

    public Result<EmailAttachment> AddAttachment(
        string fileName,
        string contentType,
        long sizeInBytes,
        byte[] content,
        string blobStorageUrl)
    {
        if (Status is InboundEmailStatus.Completed or InboundEmailStatus.Failed)
        {
            return Result.Failure<EmailAttachment>(InboundEmailErrors.CannotModifyCompletedEmail);
        }

        var contentHash = ContentHash.ComputeSha256(content);
        var metadataResult = AttachmentMetadata.Create(fileName, contentType, sizeInBytes, contentHash);

        if (metadataResult.IsFailure)
        {
            return Result.Failure<EmailAttachment>(metadataResult.Error);
        }

        var attachment = new EmailAttachment(
            Guid.NewGuid(),
            metadataResult.Value,
            blobStorageUrl);

        _attachments.Add(attachment);

        AddDomainEvent(new AttachmentExtractedEvent(
            attachment.Id,
            Id,
            fileName,
            contentType,
            sizeInBytes,
            blobStorageUrl));

        return Result.Success(attachment);
    }

    public void StartProcessing()
    {
        if (Status != InboundEmailStatus.Received)
        {
            return;
        }

        Status = InboundEmailStatus.Processing;

        AddDomainEvent(new EmailReceivedEvent(
            Id,
            Subject,
            FromAddress.Value,
            MailboxId,
            _attachments.Count));
    }

    public void CompleteProcessing()
    {
        Status = InboundEmailStatus.Completed;

        var successful = _attachments.Count(a => a.Status == EmailAttachmentStatus.Processed);
        var failed = _attachments.Count(a => a.Status == EmailAttachmentStatus.Failed);

        AddDomainEvent(new EmailProcessingCompletedEvent(
            Id,
            _attachments.Count,
            successful,
            failed));
    }

    public void FailProcessing(string errorMessage)
    {
        Status = InboundEmailStatus.Failed;
        ProcessingError = errorMessage;
    }
}

public enum InboundEmailStatus
{
    Received = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public static class InboundEmailErrors
{
    public static readonly Error InvalidTenant = new("InboundEmail.InvalidTenant", "Tenant ID is required.");
    public static readonly Error ExternalMessageIdRequired = new("InboundEmail.ExternalMessageIdRequired", "External message ID is required.");
    public static readonly Error MailboxIdRequired = new("InboundEmail.MailboxIdRequired", "Mailbox ID is required.");
    public static readonly Error CannotModifyCompletedEmail = new("InboundEmail.CannotModifyCompletedEmail", "Cannot modify a completed or failed email.");
}
