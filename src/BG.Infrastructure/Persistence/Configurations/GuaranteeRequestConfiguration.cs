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

        builder.Property(request => request.RequestedAmount)
            .HasPrecision(18, 2);

        builder.Property(request => request.Notes)
            .HasMaxLength(1000);

        builder.HasMany(request => request.Correspondence)
            .WithOne(correspondence => correspondence.GuaranteeRequest)
            .HasForeignKey(correspondence => correspondence.GuaranteeRequestId);

        builder.HasIndex(request => new { request.GuaranteeId, request.Status });
    }
}
