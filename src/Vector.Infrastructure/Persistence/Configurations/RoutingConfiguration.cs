using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vector.Domain.Routing.Aggregates;
using Vector.Domain.Routing.Entities;

namespace Vector.Infrastructure.Persistence.Configurations;

public class RoutingRuleConfiguration : IEntityTypeConfiguration<RoutingRule>
{
    public void Configure(EntityTypeBuilder<RoutingRule> builder)
    {
        builder.ToTable("RoutingRules", t => t.IsTemporal());

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(2000);

        builder.Property(r => r.Priority);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Strategy)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.TargetUnderwriterId);
        builder.Property(r => r.TargetUnderwriterName).HasMaxLength(256);
        builder.Property(r => r.TargetTeamId);
        builder.Property(r => r.TargetTeamName).HasMaxLength(256);

        builder.Property(r => r.CreatedAt);
        builder.Property(r => r.CreatedBy).HasMaxLength(256);
        builder.Property(r => r.LastModifiedAt);

        // Conditions stored as JSON
        builder.OwnsMany(r => r.Conditions, condition =>
        {
            condition.ToJson("Conditions");
            condition.Property(c => c.Field)
                .HasConversion<string>();
            condition.Property(c => c.Operator)
                .HasConversion<string>();
            condition.Property(c => c.Value);
            condition.Property(c => c.SecondaryValue);
        });

        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.Strategy);
        builder.HasIndex(r => r.Priority);

        builder.Ignore(r => r.DomainEvents);
    }
}

public class RoutingDecisionConfiguration : IEntityTypeConfiguration<RoutingDecision>
{
    public void Configure(EntityTypeBuilder<RoutingDecision> builder)
    {
        builder.ToTable("RoutingDecisions", t => t.IsTemporal());

        builder.HasKey(d => d.Id);

        builder.Property(d => d.SubmissionId)
            .IsRequired();

        builder.Property(d => d.SubmissionNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.Strategy)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.AssignedUnderwriterId);
        builder.Property(d => d.AssignedUnderwriterName).HasMaxLength(256);
        builder.Property(d => d.AssignedTeamId);
        builder.Property(d => d.AssignedTeamName).HasMaxLength(256);

        builder.Property(d => d.MatchedRuleId);
        builder.Property(d => d.MatchedRuleName).HasMaxLength(200);
        builder.Property(d => d.MatchedPairingId);

        builder.Property(d => d.RoutingReason).HasMaxLength(2000);
        builder.Property(d => d.AppetiteScore);
        builder.Property(d => d.WinnabilityScore);

        builder.Property(d => d.DecidedAt);
        builder.Property(d => d.AssignedAt);
        builder.Property(d => d.AcceptedAt);
        builder.Property(d => d.DeclinedAt);
        builder.Property(d => d.DeclineReason).HasMaxLength(2000);

        // History stored as JSON
        builder.OwnsMany(d => d.History, history =>
        {
            history.ToJson("History");
            history.Property(h => h.Timestamp);
            history.Property(h => h.Action);
            history.Property(h => h.Details);
            history.Property(h => h.Notes);
        });

        builder.HasIndex(d => d.SubmissionId).IsUnique();
        builder.HasIndex(d => d.AssignedUnderwriterId);
        builder.HasIndex(d => d.Status);

        builder.Ignore(d => d.DomainEvents);
    }
}

public class ProducerUnderwriterPairingConfiguration : IEntityTypeConfiguration<ProducerUnderwriterPairing>
{
    public void Configure(EntityTypeBuilder<ProducerUnderwriterPairing> builder)
    {
        builder.ToTable("ProducerUnderwriterPairings");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProducerId)
            .IsRequired();

        builder.Property(p => p.ProducerName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.UnderwriterId)
            .IsRequired();

        builder.Property(p => p.UnderwriterName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(p => p.Priority);
        builder.Property(p => p.IsActive);
        builder.Property(p => p.EffectiveFrom);
        builder.Property(p => p.EffectiveUntil);

        // Coverage types stored as JSON array
        builder.Property<string>("CoverageTypesJson")
            .HasColumnName("CoverageTypes")
            .HasMaxLength(1000);

        builder.HasIndex(p => p.ProducerId);
        builder.HasIndex(p => p.UnderwriterId);
        builder.HasIndex(p => new { p.ProducerId, p.IsActive });
    }
}
