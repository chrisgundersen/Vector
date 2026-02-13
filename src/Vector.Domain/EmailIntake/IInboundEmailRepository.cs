using Vector.Domain.Common;
using Vector.Domain.EmailIntake.Aggregates;
using Vector.Domain.EmailIntake.ValueObjects;

namespace Vector.Domain.EmailIntake;

/// <summary>
/// Repository interface for InboundEmail aggregate.
/// </summary>
public interface IInboundEmailRepository : IRepository<InboundEmail>
{
    /// <summary>
    /// Checks if an email with the given content hash already exists for the tenant.
    /// </summary>
    Task<bool> ExistsByContentHashAsync(Guid tenantId, ContentHash contentHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an email by its external message ID.
    /// </summary>
    Task<InboundEmail?> GetByExternalMessageIdAsync(Guid tenantId, string externalMessageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets emails pending processing for a specific mailbox.
    /// </summary>
    Task<IReadOnlyList<InboundEmail>> GetPendingByMailboxAsync(string mailboxId, int limit, CancellationToken cancellationToken = default);
}
