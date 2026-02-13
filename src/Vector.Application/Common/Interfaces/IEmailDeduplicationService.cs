namespace Vector.Application.Common.Interfaces;

/// <summary>
/// Interface for email deduplication operations to prevent duplicate processing.
/// </summary>
public interface IEmailDeduplicationService
{
    /// <summary>
    /// Checks if an email with the given content hash has already been processed.
    /// </summary>
    /// <param name="contentHash">SHA256 hash of the email content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the email has been processed; otherwise, false.</returns>
    Task<bool> IsProcessedAsync(string contentHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an email with the given content hash as processed.
    /// </summary>
    /// <param name="contentHash">SHA256 hash of the email content.</param>
    /// <param name="expiry">Optional expiry time for the processed marker. Defaults to 30 days.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsProcessedAsync(string contentHash, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific email message ID has been processed.
    /// </summary>
    /// <param name="mailboxId">The mailbox identifier.</param>
    /// <param name="messageId">The external message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the message has been processed; otherwise, false.</returns>
    Task<bool> IsMessageProcessedAsync(string mailboxId, string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a specific email message ID as processed.
    /// </summary>
    /// <param name="mailboxId">The mailbox identifier.</param>
    /// <param name="messageId">The external message ID.</param>
    /// <param name="expiry">Optional expiry time for the processed marker. Defaults to 30 days.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkMessageAsProcessedAsync(string mailboxId, string messageId, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
}
