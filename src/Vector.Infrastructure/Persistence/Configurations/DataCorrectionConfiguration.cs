using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vector.Domain.Submission.Entities;

namespace Vector.Infrastructure.Persistence.Configurations;

public class DataCorrectionRequestConfiguration : IEntityTypeConfiguration<DataCorrectionRequest>
{
    public void Configure(EntityTypeBuilder<DataCorrectionRequest> builder)
    {
        builder.ToTable("DataCorrectionRequests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.SubmissionId)
            .IsRequired();

        builder.Property(r => r.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.FieldName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.CurrentValue)
            .HasMaxLength(2000);

        builder.Property(r => r.ProposedValue)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(r => r.Justification)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.RequestedAt);
        builder.Property(r => r.RequestedBy).HasMaxLength(256);
        builder.Property(r => r.ReviewedAt);
        builder.Property(r => r.ReviewedBy).HasMaxLength(256);
        builder.Property(r => r.ReviewNotes).HasMaxLength(2000);

        builder.HasIndex(r => r.SubmissionId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => new { r.SubmissionId, r.Status });
    }
}
