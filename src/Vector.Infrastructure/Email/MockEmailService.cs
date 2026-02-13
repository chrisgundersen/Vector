using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;

namespace Vector.Infrastructure.Email;

/// <summary>
/// Mock email service for development/testing.
/// </summary>
public class MockEmailService(ILogger<MockEmailService> logger) : IEmailService
{
    private readonly List<EmailMessage> _mockEmails =
    [
        new EmailMessage(
            "msg-001",
            "Submission: ABC Manufacturing - GL/Property",
            "broker@example.com",
            "John Broker",
            "Please find attached the ACORD application for ABC Manufacturing...",
            "Please find attached the ACORD application for ABC Manufacturing. They are requesting GL and Property coverage with effective date of 4/1/2024.",
            DateTime.UtcNow.AddHours(-1),
            true,
            3),
        new EmailMessage(
            "msg-002",
            "RE: Quote Request - XYZ Contractors",
            "agent@insuranceagency.com",
            "Sarah Agent",
            "Updated loss runs attached as requested...",
            "Updated loss runs attached as requested. Please let me know if you need anything else.",
            DateTime.UtcNow.AddMinutes(-30),
            true,
            1)
    ];

    private readonly List<EmailAttachmentInfo> _mockAttachments =
    [
        new EmailAttachmentInfo("att-001", "ACORD125_ABC_Manufacturing.pdf", "application/pdf", 245000, false),
        new EmailAttachmentInfo("att-002", "LossRuns_2019-2023.pdf", "application/pdf", 180000, false),
        new EmailAttachmentInfo("att-003", "SOV_ABC_Manufacturing.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 85000, false)
    ];

    public Task<IReadOnlyList<EmailMessage>> GetNewEmailsAsync(
        string mailboxId,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Mock: Getting new emails from mailbox {MailboxId}", mailboxId);

        var emails = _mockEmails.Take(maxResults).ToList();
        return Task.FromResult<IReadOnlyList<EmailMessage>>(emails);
    }

    public Task<IReadOnlyList<EmailAttachmentInfo>> GetAttachmentsAsync(
        string mailboxId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Mock: Getting attachments for message {MessageId}", messageId);

        return Task.FromResult<IReadOnlyList<EmailAttachmentInfo>>(_mockAttachments);
    }

    public Task<byte[]> DownloadAttachmentAsync(
        string mailboxId,
        string messageId,
        string attachmentId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Mock: Downloading attachment {AttachmentId}", attachmentId);

        // Return mock PDF/Excel content
        var mockContent = new byte[1024];
        new Random().NextBytes(mockContent);

        return Task.FromResult(mockContent);
    }

    public Task MoveToProcessedAsync(
        string mailboxId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Mock: Moving message {MessageId} to processed folder", messageId);
        return Task.CompletedTask;
    }

    public Task MarkAsReadAsync(
        string mailboxId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Mock: Marking message {MessageId} as read", messageId);
        return Task.CompletedTask;
    }
}
