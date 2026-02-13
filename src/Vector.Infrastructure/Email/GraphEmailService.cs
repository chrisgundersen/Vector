using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.Messages.Item.Move;
using Vector.Application.Common.Interfaces;

namespace Vector.Infrastructure.Email;

/// <summary>
/// Microsoft Graph API email service implementation for production use.
/// </summary>
public class GraphEmailService(
    GraphServiceClient graphClient,
    IOptions<GraphEmailServiceOptions> options,
    ILogger<GraphEmailService> logger) : IEmailService
{
    private readonly GraphEmailServiceOptions _options = options.Value;

    public async Task<IReadOnlyList<EmailMessage>> GetNewEmailsAsync(
        string mailboxId,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mailboxId);

        try
        {
            logger.LogDebug(
                "Fetching up to {MaxResults} unread emails from mailbox {MailboxId}",
                maxResults,
                mailboxId);

            var messages = await graphClient.Users[mailboxId]
                .MailFolders["inbox"]
                .Messages
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = "isRead eq false";
                    requestConfiguration.QueryParameters.Top = maxResults;
                    requestConfiguration.QueryParameters.Orderby = ["receivedDateTime desc"];
                    requestConfiguration.QueryParameters.Select =
                    [
                        "id",
                        "subject",
                        "from",
                        "bodyPreview",
                        "body",
                        "receivedDateTime",
                        "hasAttachments"
                    ];
                }, cancellationToken);

            if (messages?.Value is null)
            {
                logger.LogDebug("No messages returned from mailbox {MailboxId}", mailboxId);
                return [];
            }

            var result = new List<EmailMessage>();

            foreach (var message in messages.Value)
            {
                var attachmentCount = 0;
                if (message.HasAttachments == true)
                {
                    var attachments = await graphClient.Users[mailboxId]
                        .Messages[message.Id]
                        .Attachments
                        .GetAsync(config =>
                        {
                            config.QueryParameters.Select = ["id"];
                        }, cancellationToken);

                    attachmentCount = attachments?.Value?.Count ?? 0;
                }

                result.Add(new EmailMessage(
                    message.Id ?? string.Empty,
                    message.Subject ?? string.Empty,
                    message.From?.EmailAddress?.Address ?? string.Empty,
                    message.From?.EmailAddress?.Name ?? string.Empty,
                    message.BodyPreview ?? string.Empty,
                    message.Body?.Content ?? string.Empty,
                    message.ReceivedDateTime?.UtcDateTime ?? DateTime.UtcNow,
                    message.HasAttachments ?? false,
                    attachmentCount));
            }

            logger.LogInformation(
                "Retrieved {Count} unread emails from mailbox {MailboxId}",
                result.Count,
                mailboxId);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching emails from mailbox {MailboxId}", mailboxId);
            throw;
        }
    }

    public async Task<IReadOnlyList<EmailAttachmentInfo>> GetAttachmentsAsync(
        string mailboxId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mailboxId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        try
        {
            logger.LogDebug(
                "Fetching attachments for message {MessageId} in mailbox {MailboxId}",
                messageId,
                mailboxId);

            var attachments = await graphClient.Users[mailboxId]
                .Messages[messageId]
                .Attachments
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select =
                    [
                        "id",
                        "name",
                        "contentType",
                        "size",
                        "isInline"
                    ];
                }, cancellationToken);

            if (attachments?.Value is null)
            {
                logger.LogDebug(
                    "No attachments found for message {MessageId}",
                    messageId);
                return [];
            }

            var result = attachments.Value
                .Select(a => new EmailAttachmentInfo(
                    a.Id ?? string.Empty,
                    a.Name ?? string.Empty,
                    a.ContentType ?? "application/octet-stream",
                    a.Size ?? 0,
                    a.IsInline ?? false))
                .ToList();

            logger.LogDebug(
                "Retrieved {Count} attachments for message {MessageId}",
                result.Count,
                messageId);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error fetching attachments for message {MessageId} in mailbox {MailboxId}",
                messageId,
                mailboxId);
            throw;
        }
    }

    public async Task<byte[]> DownloadAttachmentAsync(
        string mailboxId,
        string messageId,
        string attachmentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mailboxId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(attachmentId);

        try
        {
            logger.LogDebug(
                "Downloading attachment {AttachmentId} from message {MessageId}",
                attachmentId,
                messageId);

            var attachment = await graphClient.Users[mailboxId]
                .Messages[messageId]
                .Attachments[attachmentId]
                .GetAsync(cancellationToken: cancellationToken);

            if (attachment is FileAttachment fileAttachment && fileAttachment.ContentBytes is not null)
            {
                logger.LogDebug(
                    "Downloaded attachment {AttachmentId} ({Size} bytes)",
                    attachmentId,
                    fileAttachment.ContentBytes.Length);

                return fileAttachment.ContentBytes;
            }

            logger.LogWarning(
                "Attachment {AttachmentId} is not a file attachment or has no content",
                attachmentId);

            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error downloading attachment {AttachmentId} from message {MessageId}",
                attachmentId,
                messageId);
            throw;
        }
    }

    public async Task MoveToProcessedAsync(
        string mailboxId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mailboxId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        try
        {
            var processedFolderId = await GetOrCreateProcessedFolderAsync(
                mailboxId,
                cancellationToken);

            logger.LogDebug(
                "Moving message {MessageId} to processed folder {FolderId}",
                messageId,
                processedFolderId);

            var requestBody = new MovePostRequestBody
            {
                DestinationId = processedFolderId
            };

            await graphClient.Users[mailboxId]
                .Messages[messageId]
                .Move
                .PostAsync(requestBody, cancellationToken: cancellationToken);

            logger.LogInformation(
                "Moved message {MessageId} to processed folder",
                messageId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error moving message {MessageId} to processed folder in mailbox {MailboxId}",
                messageId,
                mailboxId);
            throw;
        }
    }

    public async Task MarkAsReadAsync(
        string mailboxId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mailboxId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        try
        {
            logger.LogDebug("Marking message {MessageId} as read", messageId);

            var message = new Message
            {
                IsRead = true
            };

            await graphClient.Users[mailboxId]
                .Messages[messageId]
                .PatchAsync(message, cancellationToken: cancellationToken);

            logger.LogDebug("Marked message {MessageId} as read", messageId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error marking message {MessageId} as read in mailbox {MailboxId}",
                messageId,
                mailboxId);
            throw;
        }
    }

    private async Task<string> GetOrCreateProcessedFolderAsync(
        string mailboxId,
        CancellationToken cancellationToken)
    {
        var folderName = _options.ProcessedFolderName;

        try
        {
            var folders = await graphClient.Users[mailboxId]
                .MailFolders
                .GetAsync(config =>
                {
                    config.QueryParameters.Filter = $"displayName eq '{folderName}'";
                }, cancellationToken);

            var existingFolder = folders?.Value?.FirstOrDefault();
            if (existingFolder?.Id is not null)
            {
                return existingFolder.Id;
            }

            logger.LogInformation(
                "Creating processed folder '{FolderName}' in mailbox {MailboxId}",
                folderName,
                mailboxId);

            var newFolder = new MailFolder
            {
                DisplayName = folderName
            };

            var createdFolder = await graphClient.Users[mailboxId]
                .MailFolders
                .PostAsync(newFolder, cancellationToken: cancellationToken);

            return createdFolder?.Id ??
                throw new InvalidOperationException("Failed to create processed folder");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error getting or creating processed folder in mailbox {MailboxId}",
                mailboxId);
            throw;
        }
    }
}

/// <summary>
/// Configuration options for Microsoft Graph email service.
/// </summary>
public class GraphEmailServiceOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "EmailService:Graph";

    /// <summary>
    /// Azure AD tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD application (client) ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Name of the folder to move processed emails to.
    /// </summary>
    public string ProcessedFolderName { get; set; } = "Processed";
}
