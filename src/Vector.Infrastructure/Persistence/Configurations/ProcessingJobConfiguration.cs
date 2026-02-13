using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vector.Domain.DocumentProcessing.Aggregates;

namespace Vector.Infrastructure.Persistence.Configurations;

public class ProcessingJobConfiguration : IEntityTypeConfiguration<ProcessingJob>
{
    public void Configure(EntityTypeBuilder<ProcessingJob> builder)
    {
        builder.ToTable("ProcessingJobs", t => t.IsTemporal());

        builder.HasKey(j => j.Id);

        builder.Property(j => j.TenantId)
            .IsRequired();

        builder.Property(j => j.InboundEmailId)
            .IsRequired();

        builder.Property(j => j.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(j => j.StartedAt);
        builder.Property(j => j.CompletedAt);
        builder.Property(j => j.ErrorMessage)
            .HasMaxLength(4000);

        // Audit properties
        builder.Property(j => j.CreatedAt);
        builder.Property(j => j.CreatedBy).HasMaxLength(256);
        builder.Property(j => j.LastModifiedAt);
        builder.Property(j => j.LastModifiedBy).HasMaxLength(256);

        // Documents
        builder.OwnsMany(j => j.Documents, doc =>
        {
            doc.ToTable("ProcessedDocuments", t => t.IsTemporal());

            doc.WithOwner().HasForeignKey("ProcessingJobId");
            doc.HasKey(d => d.Id);

            doc.Property(d => d.SourceAttachmentId);
            doc.Property(d => d.OriginalFileName)
                .HasMaxLength(255)
                .IsRequired();

            doc.Property(d => d.BlobStorageUrl)
                .HasMaxLength(2000)
                .IsRequired();

            doc.Property(d => d.DocumentType)
                .HasConversion<string>()
                .HasMaxLength(50);

            doc.OwnsOne(d => d.ClassificationConfidence, conf =>
            {
                conf.Property(c => c.Score)
                    .HasColumnName("ClassificationConfidence")
                    .HasPrecision(5, 4);
            });

            doc.Property(d => d.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            doc.Property(d => d.ProcessedAt);
            doc.Property(d => d.FailureReason)
                .HasMaxLength(2000);

            // Extracted fields stored as JSON
            doc.OwnsMany(d => d.ExtractedFields, field =>
            {
                field.ToTable("ExtractedFields");
                field.WithOwner().HasForeignKey("ProcessedDocumentId");

                field.Property(f => f.FieldName)
                    .HasMaxLength(200)
                    .IsRequired();

                field.Property(f => f.Value)
                    .HasMaxLength(4000);

                field.OwnsOne(f => f.Confidence, conf =>
                {
                    conf.Property(c => c.Score)
                        .HasColumnName("Confidence")
                        .HasPrecision(5, 4);
                });

                field.Property(f => f.BoundingBox)
                    .HasMaxLength(500);

                field.Property(f => f.PageNumber);
            });

            // Validation errors stored as JSON array
            doc.Property("_validationErrors")
                .HasColumnName("ValidationErrors")
                .HasColumnType("nvarchar(max)");
        });

        // Indexes
        builder.HasIndex(j => j.TenantId);
        builder.HasIndex(j => j.InboundEmailId);
        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.StartedAt);
    }
}
