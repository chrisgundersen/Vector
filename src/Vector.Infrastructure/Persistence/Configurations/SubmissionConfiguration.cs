using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vector.Domain.Submission.Aggregates;

namespace Vector.Infrastructure.Persistence.Configurations;

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("Submissions", t => t.IsTemporal());

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.TenantId)
            .IsRequired();

        builder.Property(s => s.SubmissionNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.ProcessingJobId);
        builder.Property(s => s.InboundEmailId);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.ReceivedAt);
        builder.Property(s => s.EffectiveDate);
        builder.Property(s => s.ExpirationDate);

        // Producer info
        builder.Property(s => s.ProducerId);
        builder.Property(s => s.ProducerName).HasMaxLength(500);
        builder.Property(s => s.ProducerContactEmail).HasMaxLength(256);

        // Assignment
        builder.Property(s => s.AssignedUnderwriterId);
        builder.Property(s => s.AssignedUnderwriterName).HasMaxLength(256);
        builder.Property(s => s.AssignedAt);

        // Scores
        builder.Property(s => s.AppetiteScore);
        builder.Property(s => s.WinnabilityScore);
        builder.Property(s => s.DataQualityScore);

        // Outcome
        builder.Property(s => s.DeclineReason).HasMaxLength(2000);

        builder.OwnsOne(s => s.QuotedPremium, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("QuotedPremiumAmount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("QuotedPremiumCurrency")
                .HasMaxLength(3);
        });

        // Audit properties
        builder.Property(s => s.CreatedAt);
        builder.Property(s => s.CreatedBy).HasMaxLength(256);
        builder.Property(s => s.LastModifiedAt);
        builder.Property(s => s.LastModifiedBy).HasMaxLength(256);

        // Insured Party
        builder.OwnsOne(s => s.Insured, insured =>
        {
            insured.Property(i => i.Id).HasColumnName("InsuredId");
            insured.Property(i => i.Name)
                .HasColumnName("InsuredName")
                .HasMaxLength(500)
                .IsRequired();

            insured.Property(i => i.DbaName)
                .HasColumnName("InsuredDbaName")
                .HasMaxLength(500);

            insured.Property(i => i.FeinNumber)
                .HasColumnName("InsuredFein")
                .HasMaxLength(20);

            insured.OwnsOne(i => i.MailingAddress, addr =>
            {
                addr.Property(a => a.Street1)
                    .HasColumnName("InsuredStreet1")
                    .HasMaxLength(500);

                addr.Property(a => a.Street2)
                    .HasColumnName("InsuredStreet2")
                    .HasMaxLength(500);

                addr.Property(a => a.City)
                    .HasColumnName("InsuredCity")
                    .HasMaxLength(200);

                addr.Property(a => a.State)
                    .HasColumnName("InsuredState")
                    .HasMaxLength(50);

                addr.Property(a => a.PostalCode)
                    .HasColumnName("InsuredPostalCode")
                    .HasMaxLength(20);

                addr.Property(a => a.Country)
                    .HasColumnName("InsuredCountry")
                    .HasMaxLength(100);
            });

            insured.OwnsOne(i => i.Industry, ind =>
            {
                ind.Property(n => n.NaicsCode)
                    .HasColumnName("InsuredNaicsCode")
                    .HasMaxLength(10);

                ind.Property(n => n.SicCode)
                    .HasColumnName("InsuredSicCode")
                    .HasMaxLength(10);

                ind.Property(n => n.Description)
                    .HasColumnName("InsuredIndustryDescription")
                    .HasMaxLength(500);
            });

            insured.Property(i => i.Website)
                .HasColumnName("InsuredWebsite")
                .HasMaxLength(500);

            insured.Property(i => i.YearsInBusiness)
                .HasColumnName("InsuredYearsInBusiness");

            insured.Property(i => i.EmployeeCount)
                .HasColumnName("InsuredEmployeeCount");

            insured.OwnsOne(i => i.AnnualRevenue, rev =>
            {
                rev.Property(m => m.Amount)
                    .HasColumnName("InsuredAnnualRevenueAmount")
                    .HasPrecision(18, 2);

                rev.Property(m => m.Currency)
                    .HasColumnName("InsuredAnnualRevenueCurrency")
                    .HasMaxLength(3);
            });

            insured.Property(i => i.EntityFormationDate)
                .HasColumnName("InsuredEntityFormationDate");

            insured.Property(i => i.EntityType)
                .HasColumnName("InsuredEntityType")
                .HasMaxLength(100);
        });

        // Coverages
        builder.OwnsMany(s => s.Coverages, cov =>
        {
            cov.ToTable("SubmissionCoverages", t => t.IsTemporal());
            cov.WithOwner().HasForeignKey("SubmissionId");
            cov.HasKey(c => c.Id);
            cov.Property(c => c.Id).ValueGeneratedNever();

            cov.Property(c => c.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            cov.OwnsOne(c => c.RequestedLimit, m =>
            {
                m.Property(x => x.Amount).HasColumnName("RequestedLimitAmount").HasPrecision(18, 2);
                m.Property(x => x.Currency).HasColumnName("RequestedLimitCurrency").HasMaxLength(3);
            });

            cov.OwnsOne(c => c.RequestedDeductible, m =>
            {
                m.Property(x => x.Amount).HasColumnName("RequestedDeductibleAmount").HasPrecision(18, 2);
                m.Property(x => x.Currency).HasColumnName("RequestedDeductibleCurrency").HasMaxLength(3);
            });

            cov.Property(c => c.EffectiveDate);
            cov.Property(c => c.ExpirationDate);
            cov.Property(c => c.IsCurrentlyInsured);
            cov.Property(c => c.CurrentCarrier).HasMaxLength(200);

            cov.OwnsOne(c => c.CurrentPremium, m =>
            {
                m.Property(x => x.Amount).HasColumnName("CurrentPremiumAmount").HasPrecision(18, 2);
                m.Property(x => x.Currency).HasColumnName("CurrentPremiumCurrency").HasMaxLength(3);
            });

            cov.Property(c => c.AdditionalInfo).HasMaxLength(4000);
        });

        // Locations
        builder.OwnsMany(s => s.Locations, loc =>
        {
            loc.ToTable("SubmissionLocations", t => t.IsTemporal());
            loc.WithOwner().HasForeignKey("SubmissionId");
            loc.HasKey(l => l.Id);
            loc.Property(l => l.Id).ValueGeneratedNever();

            loc.Property(l => l.LocationNumber);

            loc.OwnsOne(l => l.Address, addr =>
            {
                addr.Property(a => a.Street1).HasColumnName("Street1").HasMaxLength(500).IsRequired();
                addr.Property(a => a.Street2).HasColumnName("Street2").HasMaxLength(500);
                addr.Property(a => a.City).HasColumnName("City").HasMaxLength(200).IsRequired();
                addr.Property(a => a.State).HasColumnName("State").HasMaxLength(50).IsRequired();
                addr.Property(a => a.PostalCode).HasColumnName("PostalCode").HasMaxLength(20).IsRequired();
                addr.Property(a => a.Country).HasColumnName("Country").HasMaxLength(100).IsRequired();
            });

            loc.Property(l => l.BuildingDescription).HasMaxLength(2000);
            loc.Property(l => l.OccupancyType).HasMaxLength(200);
            loc.Property(l => l.ConstructionType).HasMaxLength(200);
            loc.Property(l => l.YearBuilt);
            loc.Property(l => l.SquareFootage);
            loc.Property(l => l.NumberOfStories);

            loc.OwnsOne(l => l.BuildingValue, m =>
            {
                m.Property(x => x.Amount).HasColumnName("BuildingValueAmount").HasPrecision(18, 2);
                m.Property(x => x.Currency).HasColumnName("BuildingValueCurrency").HasMaxLength(3);
            });

            loc.OwnsOne(l => l.ContentsValue, m =>
            {
                m.Property(x => x.Amount).HasColumnName("ContentsValueAmount").HasPrecision(18, 2);
                m.Property(x => x.Currency).HasColumnName("ContentsValueCurrency").HasMaxLength(3);
            });

            loc.OwnsOne(l => l.BusinessIncomeValue, m =>
            {
                m.Property(x => x.Amount).HasColumnName("BusinessIncomeValueAmount").HasPrecision(18, 2);
                m.Property(x => x.Currency).HasColumnName("BusinessIncomeValueCurrency").HasMaxLength(3);
            });

            loc.Property(l => l.HasSprinklers);
            loc.Property(l => l.HasFireAlarm);
            loc.Property(l => l.HasSecuritySystem);
            loc.Property(l => l.ProtectionClass).HasMaxLength(10);
        });

        // Loss History
        builder.OwnsMany(s => s.LossHistory, loss =>
        {
            loss.ToTable("SubmissionLossHistory", t => t.IsTemporal());
            loss.WithOwner().HasForeignKey("SubmissionId");
            loss.HasKey(l => l.Id);
            loss.Property(l => l.Id).ValueGeneratedNever();

            loss.Property(l => l.DateOfLoss).IsRequired();

            loss.Property(l => l.CoverageType)
                .HasConversion<string>()
                .HasMaxLength(50);

            loss.Property(l => l.ClaimNumber).HasMaxLength(100);
            loss.Property(l => l.Description).HasMaxLength(4000).IsRequired();

            loss.OwnsOne(l => l.PaidAmount, m =>
            {
                m.Property(x => x.Amount).HasColumnName("PaidAmount").HasPrecision(18, 2);
                m.Property(x => x.Currency).HasColumnName("PaidCurrency").HasMaxLength(3);
            });

            loss.OwnsOne(l => l.ReservedAmount, m =>
            {
                m.Property(x => x.Amount).HasColumnName("ReservedAmount").HasPrecision(18, 2);
                m.Property(x => x.Currency).HasColumnName("ReservedCurrency").HasMaxLength(3);
            });

            loss.OwnsOne(l => l.IncurredAmount, m =>
            {
                m.Property(x => x.Amount).HasColumnName("IncurredAmount").HasPrecision(18, 2);
                m.Property(x => x.Currency).HasColumnName("IncurredCurrency").HasMaxLength(3);
            });

            loss.Property(l => l.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            loss.Property(l => l.Carrier).HasMaxLength(200);
            loss.Property(l => l.IsSubrogation);
        });

        // Indexes
        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => new { s.TenantId, s.SubmissionNumber }).IsUnique();
        builder.HasIndex(s => s.ProcessingJobId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.AssignedUnderwriterId);
        builder.HasIndex(s => s.ProducerId);
        builder.HasIndex(s => s.ReceivedAt);
    }
}
