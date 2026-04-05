using BG.Domain.Guarantees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

public sealed class GuaranteeRequestConfiguration : IEntityTypeConfiguration<GuaranteeRequest>
{
    public void Configure(EntityTypeBuilder<GuaranteeRequest> builder)
    {
        builder.ToTable("guarantee_requests");

        builder.HasKey(request => request.Id);

        builder.Property(request => request.RequestType)
            .HasConversion<string>()
            .HasMaxLength(48)
            .IsRequired();

        builder.Property(request => request.Status)
            .HasConversion<string>()
            .HasMaxLength(48)
            .IsRequired();

        builder.Property(request => request.RequestChannel)
            .HasConversion<string>()
            .HasMaxLength(48)
            .IsRequired();

        builder.Property(request => request.RequestedAmount)
            .HasPrecision(18, 2);

        builder.Property(request => request.Notes)
            .HasMaxLength(1000);

        builder.HasOne(request => request.RequestedByUser)
            .WithMany()
            .HasForeignKey(request => request.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(request => request.Correspondence)
            .WithOne(correspondence => correspondence.GuaranteeRequest)
            .HasForeignKey(correspondence => correspondence.GuaranteeRequestId);

        builder.HasMany(request => request.RequestDocuments)
            .WithOne(link => link.GuaranteeRequest)
            .HasForeignKey(link => link.GuaranteeRequestId);

        builder.HasOne(request => request.ApprovalProcess)
            .WithOne(process => process.GuaranteeRequest)
            .HasForeignKey<BG.Domain.Workflow.RequestApprovalProcess>(process => process.GuaranteeRequestId);

        builder.HasIndex(request => new { request.RequestedByUserId, request.Status });
        builder.HasIndex(request => new { request.GuaranteeId, request.Status });

        // Prevent race-condition duplicates for open requests of the same type on the same guarantee/user.
        builder.HasIndex(request => new { request.RequestedByUserId, request.GuaranteeId, request.RequestType })
            .HasDatabaseName("IX_guarantee_requests_open_owner_guarantee_type_unique")
            .IsUnique()
            .HasFilter("\"Status\" <> 'Completed' AND \"Status\" <> 'Cancelled' AND \"Status\" <> 'Rejected'");
    }
}
