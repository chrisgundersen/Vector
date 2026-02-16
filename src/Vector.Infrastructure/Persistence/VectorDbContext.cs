using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;
using Vector.Domain.DocumentProcessing.Aggregates;
using Vector.Domain.EmailIntake.Aggregates;
using Vector.Domain.Routing.Aggregates;
using Vector.Domain.Routing.Entities;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Entities;
using Vector.Domain.UnderwritingGuidelines.Aggregates;

namespace Vector.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for Vector.
/// </summary>
public class VectorDbContext(
    DbContextOptions<VectorDbContext> options,
    ICurrentUserService currentUserService,
    IDomainEventDispatcher? domainEventDispatcher = null) : DbContext(options), IUnitOfWork
{
    public DbSet<InboundEmail> InboundEmails => Set<InboundEmail>();
    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<UnderwritingGuideline> UnderwritingGuidelines => Set<UnderwritingGuideline>();
    public DbSet<RoutingRule> RoutingRules => Set<RoutingRule>();
    public DbSet<RoutingDecision> RoutingDecisions => Set<RoutingDecision>();
    public DbSet<ProducerUnderwriterPairing> ProducerUnderwriterPairings => Set<ProducerUnderwriterPairing>();
    public DbSet<DataCorrectionRequest> DataCorrectionRequests => Set<DataCorrectionRequest>();

    private Guid? CurrentTenantId => currentUserService.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VectorDbContext).Assembly);

        // Remove temporal table configuration for SQLite (not supported)
        if (Database.IsSqlite())
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // SQLite doesn't support temporal tables
                entityType.SetIsTemporal(false);
            }
        }

        // Apply multi-tenant query filters
        modelBuilder.Entity<InboundEmail>()
            .HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);

        modelBuilder.Entity<ProcessingJob>()
            .HasQueryFilter(j => CurrentTenantId == null || j.TenantId == CurrentTenantId);

        modelBuilder.Entity<Submission>()
            .HasQueryFilter(s => CurrentTenantId == null || s.TenantId == CurrentTenantId);

        modelBuilder.Entity<UnderwritingGuideline>()
            .HasQueryFilter(g => CurrentTenantId == null || g.TenantId == CurrentTenantId);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();

        var domainEvents = CollectDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (domainEventDispatcher is not null && domainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchEventsAsync(domainEvents, cancellationToken);
        }

        return result;
    }

    private List<IDomainEvent> CollectDomainEvents()
    {
        var aggregatesWithEvents = ChangeTracker.Entries<AggregateRoot<Guid>>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregatesWithEvents
            .SelectMany(a => a.DomainEvents)
            .ToList();

        foreach (var aggregate in aggregatesWithEvents)
        {
            aggregate.ClearDomainEvents();
        }

        return domainEvents;
    }

    private void UpdateAuditableEntities()
    {
        var entries = ChangeTracker.Entries<AuditableAggregateRoot>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.SetCreatedAudit(currentUserService.UserId);
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.SetModifiedAudit(currentUserService.UserId);
            }
        }

        var entriesWithGuid = ChangeTracker.Entries<AuditableAggregateRoot<Guid>>();
        foreach (var entry in entriesWithGuid)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.SetCreatedAudit(currentUserService.UserId);
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.SetModifiedAudit(currentUserService.UserId);
            }
        }
    }
}
