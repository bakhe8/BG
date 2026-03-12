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

        builder.Property(document => document.CapturedByDisplayName)
            .HasMaxLength(256);

        builder.Property(document => document.CaptureChannel)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(document => document.SourceSystemName)
            .HasMaxLength(128);

        builder.Property(document => document.SourceReference)
            .HasMaxLength(128);

        builder.Property(document => document.FileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(document => document.StoragePath)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(document => document.IntakeScenarioKey)
            .HasMaxLength(64);

        builder.Property(document => document.ExtractionMethod)
            .HasMaxLength(64);

        builder.Property(document => document.VerifiedDataJson)
            .HasColumnType("text");

        builder.Property(document => document.Notes)
            .HasMaxLength(1000);

        builder.HasOne(document => document.CapturedByUser)
            .WithMany()
            .HasForeignKey(document => document.CapturedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(document => document.RequestLinks)
            .WithOne(link => link.GuaranteeDocument)
            .HasForeignKey(link => link.GuaranteeDocumentId);

        builder.HasIndex(document => new { document.GuaranteeId, document.CapturedAtUtc });
    }
}
