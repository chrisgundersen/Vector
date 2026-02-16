using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vector.Domain.EmailIntake.Aggregates;

namespace Vector.Infrastructure.Persistence.Configurations;

public class InboundEmailConfiguration : IEntityTypeConfiguration<InboundEmail>
{
    public void Configure(EntityTypeBuilder<InboundEmail> builder)
    {
        builder.ToTable("InboundEmails", t => t.IsTemporal());

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.ExternalMessageId)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.MailboxId)
            .HasMaxLength(200)
            .IsRequired();

        builder.OwnsOne(e => e.FromAddress, address =>
        {
            address.Property(a => a.Value)
                .HasColumnName("FromAddress")
                .HasMaxLength(256)
                .IsRequired();
        });

        builder.Property(e => e.Subject)
            .HasMaxLength(1000);

        builder.Property(e => e.BodyPreview)
            .HasMaxLength(4000);

        builder.OwnsOne(e => e.ContentHash, hash =>
        {
            hash.Property(h => h.Value)
                .HasColumnName("ContentHash")
                .HasMaxLength(128)
                .IsRequired();

            hash.Property(h => h.Algorithm)
                .HasColumnName("ContentHashAlgorithm")
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.Property(e => e.ReceivedAt)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ProcessingError)
            .HasMaxLength(2000);

        // Audit properties
        builder.Property(e => e.CreatedAt);
        builder.Property(e => e.CreatedBy).HasMaxLength(256);
        builder.Property(e => e.LastModifiedAt);
        builder.Property(e => e.LastModifiedBy).HasMaxLength(256);

        // Attachments
        builder.OwnsMany(e => e.Attachments, attachment =>
        {
            attachment.ToTable("EmailAttachments", t => t.IsTemporal());

            attachment.WithOwner().HasForeignKey("InboundEmailId");
            attachment.HasKey(a => a.Id);
            attachment.Property(a => a.Id).ValueGeneratedNever();

            attachment.Property(a => a.BlobStorageUrl)
                .HasMaxLength(2000)
                .IsRequired();

            attachment.Property(a => a.ExtractedAt);
            attachment.Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            attachment.Property(a => a.FailureReason)
                .HasMaxLength(2000);

            attachment.OwnsOne(a => a.Metadata, metadata =>
            {
                metadata.Property(m => m.FileName)
                    .HasColumnName("FileName")
                    .HasMaxLength(255)
                    .IsRequired();

                metadata.Property(m => m.ContentType)
                    .HasColumnName("ContentType")
                    .HasMaxLength(200)
                    .IsRequired();

                metadata.Property(m => m.SizeInBytes)
                    .HasColumnName("SizeInBytes");

                metadata.OwnsOne(m => m.ContentHash, hash =>
                {
                    hash.Property(h => h.Value)
                        .HasColumnName("ContentHash")
                        .HasMaxLength(128)
                        .IsRequired();

                    hash.Property(h => h.Algorithm)
                        .HasColumnName("ContentHashAlgorithm")
                        .HasMaxLength(20)
                        .IsRequired();
                });
            });
        });

        // Indexes
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.ExternalMessageId }).IsUnique();
        builder.HasIndex(e => e.MailboxId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.ReceivedAt);
    }
}
