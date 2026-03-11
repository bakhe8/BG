using BG.Domain.Guarantees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

public sealed class GuaranteeCorrespondenceConfiguration : IEntityTypeConfiguration<GuaranteeCorrespondence>
{
    public void Configure(EntityTypeBuilder<GuaranteeCorrespondence> builder)
    {
        builder.ToTable("guarantee_correspondence");

        builder.HasKey(correspondence => correspondence.Id);

        builder.Property(correspondence => correspondence.Direction)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(correspondence => correspondence.Kind)
            .HasConversion<string>()
            .HasMaxLength(48)
            .IsRequired();

        builder.Property(correspondence => correspondence.ReferenceNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(correspondence => correspondence.Notes)
            .HasMaxLength(1000);

        builder.HasOne(correspondence => correspondence.ScannedDocument)
            .WithMany(document => document.Correspondence)
            .HasForeignKey(correspondence => correspondence.ScannedDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(correspondence => new { correspondence.GuaranteeId, correspondence.LetterDate });
    }
}
