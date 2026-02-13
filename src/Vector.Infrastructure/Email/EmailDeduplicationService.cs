using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;

namespace Vector.Infrastructure.Email;

/// <summary>
/// Redis-based email deduplication service to prevent duplicate email processing.
/// </summary>
public class EmailDeduplicationService(
    ICacheService cacheService,
    ILogger<EmailDeduplicationService> logger) : IEmailDeduplicationService
{
    private const string ContentHashKeyPrefix = "email:hash:";
    private const string MessageIdKeyPrefix = "email:msg:";
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromDays(30);

    public async Task<bool> IsProcessedAsync(
        string contentHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);

        var key = BuildContentHashKey(contentHash);
        var exists = await cacheService.ExistsAsync(key, cancellationToken);

        logger.LogDebug(
            "Content hash {ContentHash} processed check: {IsProcessed}",
            contentHash[..Math.Min(12, contentHash.Length)],
            exists);

        return exists;
    }

    public async Task MarkAsProcessedAsync(
        string contentHash,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);

        var key = BuildContentHashKey(contentHash);
        var effectiveExpiry = expiry ?? DefaultExpiry;
        var processedInfo = new ProcessedEmailInfo(
            ContentHash: contentHash,
            ProcessedAt: DateTime.UtcNow);

        await cacheService.SetAsync(key, processedInfo, effectiveExpiry, cancellationToken);

        logger.LogDebug(
            "Marked content hash {ContentHash} as processed (expires in {Expiry})",
            contentHash[..Math.Min(12, contentHash.Length)],
            effectiveExpiry);
    }

    public async Task<bool> IsMessageProcessedAsync(
        string mailboxId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mailboxId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var key = BuildMessageIdKey(mailboxId, messageId);
        var exists = await cacheService.ExistsAsync(key, cancellationToken);

        logger.LogDebug(
            "Message {MessageId} in mailbox {MailboxId} processed check: {IsProcessed}",
            messageId,
            mailboxId,
            exists);

        return exists;
    }

    public async Task MarkMessageAsProcessedAsync(
        string mailboxId,
        string messageId,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mailboxId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var key = BuildMessageIdKey(mailboxId, messageId);
        var effectiveExpiry = expiry ?? DefaultExpiry;
        var processedInfo = new ProcessedEmailInfo(
            ContentHash: null,
            ProcessedAt: DateTime.UtcNow,
            MailboxId: mailboxId,
            MessageId: messageId);

        await cacheService.SetAsync(key, processedInfo, effectiveExpiry, cancellationToken);

        logger.LogDebug(
            "Marked message {MessageId} in mailbox {MailboxId} as processed (expires in {Expiry})",
            messageId,
            mailboxId,
            effectiveExpiry);
    }

    private static string BuildContentHashKey(string contentHash)
        => $"{ContentHashKeyPrefix}{contentHash}";

    private static string BuildMessageIdKey(string mailboxId, string messageId)
        => $"{MessageIdKeyPrefix}{mailboxId}:{messageId}";

    private sealed record ProcessedEmailInfo(
        string? ContentHash,
        DateTime ProcessedAt,
        string? MailboxId = null,
        string? MessageId = null);
}
