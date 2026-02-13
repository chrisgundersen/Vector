using Microsoft.EntityFrameworkCore;
using Vector.Domain.Common;
using Vector.Domain.EmailIntake;
using Vector.Domain.EmailIntake.Aggregates;
using Vector.Domain.EmailIntake.ValueObjects;

namespace Vector.Infrastructure.Persistence.Repositories;

public class InboundEmailRepository(VectorDbContext context) : IInboundEmailRepository
{
    public IUnitOfWork UnitOfWork => context;

    public async Task<InboundEmail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.InboundEmails
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task AddAsync(InboundEmail aggregate, CancellationToken cancellationToken = default)
    {
        await context.InboundEmails.AddAsync(aggregate, cancellationToken);
    }

    public void Update(InboundEmail aggregate)
    {
        context.InboundEmails.Update(aggregate);
    }

    public void Remove(InboundEmail aggregate)
    {
        context.InboundEmails.Remove(aggregate);
    }

    public async Task<bool> ExistsByContentHashAsync(
        Guid tenantId,
        ContentHash contentHash,
        CancellationToken cancellationToken = default)
    {
        return await context.InboundEmails
            .IgnoreQueryFilters()
            .AnyAsync(e =>
                e.TenantId == tenantId &&
                e.ContentHash.Value == contentHash.Value,
                cancellationToken);
    }

    public async Task<InboundEmail?> GetByExternalMessageIdAsync(
        Guid tenantId,
        string externalMessageId,
        CancellationToken cancellationToken = default)
    {
        return await context.InboundEmails
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e =>
                e.TenantId == tenantId &&
                e.ExternalMessageId == externalMessageId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<InboundEmail>> GetPendingByMailboxAsync(
        string mailboxId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await context.InboundEmails
            .Where(e =>
                e.MailboxId == mailboxId &&
                e.Status == InboundEmailStatus.Received)
            .OrderBy(e => e.ReceivedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
