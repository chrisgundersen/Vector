using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vector.Domain.UnderwritingGuidelines.Aggregates;
using Vector.Domain.UnderwritingGuidelines.Entities;
using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Infrastructure.Persistence.Configurations;

public class UnderwritingGuidelineConfiguration : IEntityTypeConfiguration<UnderwritingGuideline>
{
    public void Configure(EntityTypeBuilder<UnderwritingGuideline> builder)
    {
        builder.ToTable("UnderwritingGuidelines", t => t.IsTemporal());

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();

        builder.Property(g => g.TenantId)
            .IsRequired();

        builder.Property(g => g.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(g => g.Description)
            .HasMaxLength(2000);

        builder.Property(g => g.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(g => g.EffectiveDate);
        builder.Property(g => g.ExpirationDate);
        builder.Property(g => g.Version);

        builder.Property(g => g.ApplicableCoverageTypes)
            .HasMaxLength(500);

        builder.Property(g => g.ApplicableStates)
            .HasMaxLength(500);

        builder.Property(g => g.ApplicableNAICSCodes)
            .HasMaxLength(500);

        // Audit fields
        builder.Property(g => g.CreatedAt);
        builder.Property(g => g.CreatedBy).HasMaxLength(256);
        builder.Property(g => g.LastModifiedAt);
        builder.Property(g => g.LastModifiedBy).HasMaxLength(256);

        // Rules relationship
        builder.HasMany(g => g.Rules)
            .WithOne()
            .HasForeignKey("GuidelineId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(g => g.TenantId);
        builder.HasIndex(g => g.Status);
        builder.HasIndex(g => new { g.TenantId, g.Status });

        builder.Ignore(g => g.DomainEvents);
    }
}

public class UnderwritingRuleConfiguration : IEntityTypeConfiguration<UnderwritingRule>
{
    public void Configure(EntityTypeBuilder<UnderwritingRule> builder)
    {
        builder.ToTable("UnderwritingRules");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(2000);

        builder.Property(r => r.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Action)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Priority);
        builder.Property(r => r.IsActive);
        builder.Property(r => r.ScoreAdjustment);
        builder.Property(r => r.PricingModifier).HasPrecision(5, 4);
        builder.Property(r => r.Message).HasMaxLength(1000);

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
    }
}
