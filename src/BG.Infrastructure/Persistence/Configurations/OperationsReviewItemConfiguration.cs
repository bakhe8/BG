using BG.Domain.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

public sealed class OperationsReviewItemConfiguration : IEntityTypeConfiguration<OperationsReviewItem>
{
    public void Configure(EntityTypeBuilder<OperationsReviewItem> builder)
    {
        builder.ToTable("operations_review_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.GuaranteeNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.ScenarioKey)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.Category)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(item => item.RoutedToLaneKey)
            .HasMaxLength(64);

        builder.HasOne(item => item.Guarantee)
            .WithMany()
            .HasForeignKey(item => item.GuaranteeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.GuaranteeDocument)
            .WithMany()
            .HasForeignKey(item => item.GuaranteeDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.GuaranteeCorrespondence)
            .WithMany()
            .HasForeignKey(item => item.GuaranteeCorrespondenceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(item => new { item.Status, item.CreatedAtUtc });
        builder.HasIndex(item => new { item.GuaranteeId, item.Status });
    }
}
