using BG.Domain.Guarantees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

public sealed class GuaranteeConfiguration : IEntityTypeConfiguration<Guarantee>
{
    public void Configure(EntityTypeBuilder<Guarantee> builder)
    {
        builder.ToTable("guarantees");

        builder.HasKey(guarantee => guarantee.Id);

        builder.Property(guarantee => guarantee.GuaranteeNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(guarantee => guarantee.BankName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(guarantee => guarantee.BeneficiaryName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(guarantee => guarantee.PrincipalName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(guarantee => guarantee.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(guarantee => guarantee.CurrentAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(guarantee => guarantee.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(guarantee => guarantee.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(guarantee => guarantee.ExternalReference)
            .HasMaxLength(128);

        builder.Property(guarantee => guarantee.SupersededByGuaranteeNumber)
            .HasMaxLength(64);

        builder.HasIndex(guarantee => guarantee.GuaranteeNumber)
            .IsUnique();

        builder.HasMany(guarantee => guarantee.Documents)
            .WithOne(document => document.Guarantee)
            .HasForeignKey(document => document.GuaranteeId);

        builder.HasMany(guarantee => guarantee.Requests)
            .WithOne(request => request.Guarantee)
            .HasForeignKey(request => request.GuaranteeId);

        builder.HasMany(guarantee => guarantee.Correspondence)
            .WithOne(correspondence => correspondence.Guarantee)
            .HasForeignKey(correspondence => correspondence.GuaranteeId);

        builder.HasMany(guarantee => guarantee.Events)
            .WithOne(guaranteeEvent => guaranteeEvent.Guarantee)
            .HasForeignKey(guaranteeEvent => guaranteeEvent.GuaranteeId);
    }
}
