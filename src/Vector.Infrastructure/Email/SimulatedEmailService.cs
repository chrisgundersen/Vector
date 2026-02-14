using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;

namespace Vector.Infrastructure.Email;

/// <summary>
/// Email service that supports simulated email injection for local development and testing.
/// Emails can be added via the AddSimulatedEmail method and will be returned by GetNewEmailsAsync.
/// </summary>
public class SimulatedEmailService : IEmailService, ISimulatedEmailService
{
    private readonly ILogger<SimulatedEmailService> _logger;
    private readonly ConcurrentQueue<EmailMessage> _pendingEmails = new();
    private readonly ConcurrentDictionary<string, EmailMessage> _processedEmails = new();
    private readonly ConcurrentDictionary<string, List<EmailAttachmentInfo>> _attachments = new();
    private readonly ConcurrentDictionary<string, SimulatedAttachment> _attachmentContent = new();
    private int _emailCounter;

    public SimulatedEmailService(ILogger<SimulatedEmailService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds a simulated email to the queue for processing.
    /// </summary>
    public string AddSimulatedEmail(SimulatedEmailRequest request)
    {
        var messageId = $"SIM-{Interlocked.Increment(ref _emailCounter):D6}";

        var email = new EmailMessage(
            messageId,
            request.Subject,
            request.FromAddress,
            request.FromName ?? request.FromAddress,
            request.Body.Length > 200 ? request.Body[..200] : request.Body,
            request.Body,
            DateTime.UtcNow,
            request.Attachments?.Count > 0,
            request.Attachments?.Count ?? 0);

        _pendingEmails.Enqueue(email);

        // Store attachments if any
        if (request.Attachments?.Count > 0)
        {
            var attachments = request.Attachments.Select((a, i) =>
            {
                var attachmentId = $"{messageId}-ATT-{i:D3}";

                // Store the attachment content for later download
                _attachmentContent[attachmentId] = a;

                return new EmailAttachmentInfo(
                    attachmentId,
                    a.FileName,
                    a.ContentType ?? GetContentType(a.FileName),
                    a.Content?.Length ?? a.Base64Content?.Length ?? 1024,
                    false);
            }).ToList();

            _attachments[messageId] = attachments;
        }

        _logger.LogInformation(
            "Simulated email added: {MessageId} from {From} - {Subject} ({AttachmentCount} attachments)",
            messageId, request.FromAddress, request.Subject, request.Attachments?.Count ?? 0);

        return messageId;
    }

    /// <summary>
    /// Gets the list of pending simulated emails.
    /// </summary>
    public IReadOnlyList<EmailMessage> GetPendingEmails()
    {
        return _pendingEmails.ToArray();
    }

    /// <summary>
    /// Gets the list of processed emails.
    /// </summary>
    public IReadOnlyList<EmailMessage> GetProcessedEmails()
    {
        return _processedEmails.Values.ToList();
    }

    /// <summary>
    /// Clears all simulated emails (pending and processed).
    /// </summary>
    public void ClearAll()
    {
        while (_pendingEmails.TryDequeue(out _)) { }
        _processedEmails.Clear();
        _attachments.Clear();
        _attachmentContent.Clear();
        _logger.LogInformation("All simulated emails cleared");
    }

    public Task<IReadOnlyList<EmailMessage>> GetNewEmailsAsync(
        string mailboxId,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var emails = new List<EmailMessage>();

        while (emails.Count < maxResults && _pendingEmails.TryDequeue(out var email))
        {
            emails.Add(email);
            _logger.LogDebug("Retrieved simulated email: {MessageId}", email.MessageId);
        }

        _logger.LogInformation(
            "GetNewEmailsAsync for {Mailbox}: returned {Count} simulated emails",
            mailboxId, emails.Count);

        return Task.FromResult<IReadOnlyList<EmailMessage>>(emails);
    }

    public Task<IReadOnlyList<EmailAttachmentInfo>> GetAttachmentsAsync(
        string mailboxId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        if (_attachments.TryGetValue(messageId, out var attachments))
        {
            _logger.LogDebug(
                "GetAttachmentsAsync for {MessageId}: returned {Count} attachments",
                messageId, attachments.Count);
            return Task.FromResult<IReadOnlyList<EmailAttachmentInfo>>(attachments);
        }

        return Task.FromResult<IReadOnlyList<EmailAttachmentInfo>>(Array.Empty<EmailAttachmentInfo>());
    }

    public Task<byte[]> DownloadAttachmentAsync(
        string mailboxId,
        string messageId,
        string attachmentId,
        CancellationToken cancellationToken = default)
    {
        // Check if we have actual content stored
        if (_attachmentContent.TryGetValue(attachmentId, out var attachment))
        {
            if (attachment.Content is not null)
            {
                _logger.LogDebug(
                    "DownloadAttachmentAsync for {AttachmentId}: returned stored content ({Size} bytes)",
                    attachmentId, attachment.Content.Length);
                return Task.FromResult(attachment.Content);
            }

            if (!string.IsNullOrEmpty(attachment.Base64Content))
            {
                var content = Convert.FromBase64String(attachment.Base64Content);
                _logger.LogDebug(
                    "DownloadAttachmentAsync for {AttachmentId}: returned base64 content ({Size} bytes)",
                    attachmentId, content.Length);
                return Task.FromResult(content);
            }
        }

        // Return sample PDF content for simulated attachments
        var sampleContent = GenerateSampleDocumentContent(attachmentId);
        _logger.LogDebug(
            "DownloadAttachmentAsync for {AttachmentId}: returned sample content ({Size} bytes)",
            attachmentId, sampleContent.Length);
        return Task.FromResult(sampleContent);
    }

    public Task MoveToProcessedAsync(
        string mailboxId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Email {MessageId} moved to processed folder for mailbox {Mailbox}",
            messageId, mailboxId);
        return Task.CompletedTask;
    }

    public Task MarkAsReadAsync(
        string mailboxId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Email {MessageId} marked as read", messageId);
        return Task.CompletedTask;
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".tif" or ".tiff" => "image/tiff",
            _ => "application/octet-stream"
        };
    }

    private static byte[] GenerateSampleDocumentContent(string attachmentId)
    {
        // Generate a simple PDF-like content for testing
        // In a real scenario, you might load actual sample files
        var content = $"%PDF-1.4\n1 0 obj\n<<\n/Type /Catalog\n>>\nendobj\n%%EOF\n% Simulated attachment: {attachmentId}\n";
        return System.Text.Encoding.UTF8.GetBytes(content);
    }
}

/// <summary>
/// Interface for simulated email operations.
/// </summary>
public interface ISimulatedEmailService
{
    string AddSimulatedEmail(SimulatedEmailRequest request);
    IReadOnlyList<EmailMessage> GetPendingEmails();
    IReadOnlyList<EmailMessage> GetProcessedEmails();
    void ClearAll();
}

/// <summary>
/// Request model for creating a simulated email.
/// </summary>
public record SimulatedEmailRequest
{
    public string FromAddress { get; init; } = "producer@example.com";
    public string? FromName { get; init; }
    public string Subject { get; init; } = "New Submission";
    public string Body { get; init; } = "Please find attached the submission documents.";
    public bool IsHtml { get; init; } = false;
    public List<SimulatedAttachment>? Attachments { get; init; }
}

/// <summary>
/// Attachment model for simulated emails.
/// </summary>
public record SimulatedAttachment
{
    public string FileName { get; init; } = "document.pdf";
    public string? ContentType { get; init; }
    public byte[]? Content { get; init; }
    public string? Base64Content { get; init; }
}
