using BG.Domain.Guarantees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

public sealed class GuaranteeDocumentConfiguration : IEntityTypeConfiguration<GuaranteeDocument>
{
    public void Configure(EntityTypeBuilder<GuaranteeDocument> builder)
    {
        builder.ToTable("guarantee_documents");

        builder.HasKey(document => document.Id);

        builder.Property(document => document.DocumentType)
            .HasConversion<string>()
            .HasMaxLength(48)
            .IsRequired();

        builder.Property(document => document.SourceType)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(document => document.FileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(document => document.StoragePath)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(document => document.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(document => new { document.GuaranteeId, document.CapturedAtUtc });
    }
}
