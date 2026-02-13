namespace Vector.Application.Common.Interfaces;

/// <summary>
/// Interface for email operations via Microsoft Graph.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Gets new emails from a shared mailbox.
    /// </summary>
    Task<IReadOnlyList<EmailMessage>> GetNewEmailsAsync(
        string mailboxId,
        int maxResults,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets attachments for a specific email.
    /// </summary>
    Task<IReadOnlyList<EmailAttachmentInfo>> GetAttachmentsAsync(
        string mailboxId,
        string messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads an attachment's content.
    /// </summary>
    Task<byte[]> DownloadAttachmentAsync(
        string mailboxId,
        string messageId,
        string attachmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves an email to a processed folder.
    /// </summary>
    Task MoveToProcessedAsync(
        string mailboxId,
        string messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an email as read.
    /// </summary>
    Task MarkAsReadAsync(
        string mailboxId,
        string messageId,
        CancellationToken cancellationToken = default);
}

public record EmailMessage(
    string MessageId,
    string Subject,
    string FromAddress,
    string FromName,
    string BodyPreview,
    string BodyContent,
    DateTime ReceivedDateTime,
    bool HasAttachments,
    int AttachmentCount);

public record EmailAttachmentInfo(
    string AttachmentId,
    string FileName,
    string ContentType,
    long Size,
    bool IsInline);
