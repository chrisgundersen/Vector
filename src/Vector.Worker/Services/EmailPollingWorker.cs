using MediatR;
using Vector.Application.Common.Interfaces;
using Vector.Application.EmailIntake.Commands;

namespace Vector.Worker.Services;

/// <summary>
/// Background worker that polls shared mailboxes for new emails.
/// </summary>
public class EmailPollingWorker(
    IServiceScopeFactory scopeFactory,
    IEmailService emailService,
    IEmailDeduplicationService deduplicationService,
    ILogger<EmailPollingWorker> logger,
    IConfiguration configuration) : BackgroundService
{
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(
        configuration.GetValue("EmailPolling:IntervalSeconds", 60));

    private readonly int _maxEmailsPerPoll = configuration.GetValue("EmailPolling:MaxEmailsPerPoll", 10);

    private readonly string[] _mailboxIds = configuration
        .GetSection("EmailPolling:MailboxIds")
        .Get<string[]>() ?? ["submissions@vector.local"];

    private readonly Guid _defaultTenantId = Guid.Parse(
        configuration.GetValue("EmailPolling:DefaultTenantId", "00000000-0000-0000-0000-000000000001")!);

    private readonly bool _enableMessageDeduplication = configuration.GetValue("EmailPolling:EnableMessageDeduplication", true);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email Polling Worker starting. Interval: {Interval}s, Mailboxes: {Mailboxes}",
            _pollingInterval.TotalSeconds, string.Join(", ", _mailboxIds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollMailboxesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during email polling cycle");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task PollMailboxesAsync(CancellationToken cancellationToken)
    {
        foreach (var mailboxId in _mailboxIds)
        {
            try
            {
                await ProcessMailboxAsync(mailboxId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing mailbox {MailboxId}", mailboxId);
            }
        }
    }

    private async Task ProcessMailboxAsync(string mailboxId, CancellationToken cancellationToken)
    {
        logger.LogDebug("Polling mailbox {MailboxId}", mailboxId);

        var emails = await emailService.GetNewEmailsAsync(mailboxId, _maxEmailsPerPoll, cancellationToken);

        if (emails.Count == 0)
        {
            logger.LogDebug("No new emails in mailbox {MailboxId}", mailboxId);
            return;
        }

        logger.LogInformation("Found {Count} new emails in mailbox {MailboxId}", emails.Count, mailboxId);

        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        foreach (var email in emails)
        {
            try
            {
                // Check for duplicate processing by message ID
                if (_enableMessageDeduplication)
                {
                    var isProcessed = await deduplicationService.IsMessageProcessedAsync(
                        mailboxId,
                        email.MessageId,
                        cancellationToken);

                    if (isProcessed)
                    {
                        logger.LogDebug(
                            "Skipping already processed message {MessageId} in mailbox {MailboxId}",
                            email.MessageId,
                            mailboxId);
                        continue;
                    }
                }

                await ProcessEmailAsync(mediator, mailboxId, email, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing email {MessageId}", email.MessageId);
            }
        }
    }

    private async Task ProcessEmailAsync(
        IMediator mediator,
        string mailboxId,
        EmailMessage email,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing email {MessageId}: {Subject}", email.MessageId, email.Subject);

        var command = new ProcessInboundEmailCommand(
            _defaultTenantId,
            mailboxId,
            email.MessageId,
            email.FromAddress,
            email.Subject,
            email.BodyPreview,
            email.BodyContent,
            email.ReceivedDateTime);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Failed to process email {MessageId}: {Error}",
                email.MessageId, result.Error.Description);
            return;
        }

        var emailId = result.Value;
        logger.LogInformation("Created inbound email {EmailId} from message {MessageId}", emailId, email.MessageId);

        // Extract attachments
        if (email.HasAttachments)
        {
            await ExtractAttachmentsAsync(mediator, mailboxId, email.MessageId, emailId, cancellationToken);
        }

        // Mark as processed in email service
        await emailService.MarkAsReadAsync(mailboxId, email.MessageId, cancellationToken);
        await emailService.MoveToProcessedAsync(mailboxId, email.MessageId, cancellationToken);

        // Mark as processed in deduplication service for future runs
        if (_enableMessageDeduplication)
        {
            await deduplicationService.MarkMessageAsProcessedAsync(
                mailboxId,
                email.MessageId,
                cancellationToken: cancellationToken);
        }
    }

    private async Task ExtractAttachmentsAsync(
        IMediator mediator,
        string mailboxId,
        string messageId,
        Guid emailId,
        CancellationToken cancellationToken)
    {
        var attachments = await emailService.GetAttachmentsAsync(mailboxId, messageId, cancellationToken);

        foreach (var attachment in attachments.Where(a => !a.IsInline))
        {
            try
            {
                var content = await emailService.DownloadAttachmentAsync(
                    mailboxId, messageId, attachment.AttachmentId, cancellationToken);

                var command = new ExtractAttachmentCommand(
                    emailId,
                    attachment.FileName,
                    attachment.ContentType,
                    content);

                var result = await mediator.Send(command, cancellationToken);

                if (result.IsFailure)
                {
                    logger.LogWarning("Failed to extract attachment {FileName}: {Error}",
                        attachment.FileName, result.Error.Description);
                }
                else
                {
                    logger.LogInformation("Extracted attachment {AttachmentId}: {FileName}",
                        result.Value, attachment.FileName);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error extracting attachment {FileName}", attachment.FileName);
            }
        }
    }
}
