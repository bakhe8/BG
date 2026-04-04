using System.Text.Json;
using BG.Domain.Guarantees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

public sealed class BankConfiguration : IEntityTypeConfiguration<Bank>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<Bank> builder)
    {
        builder.ToTable("banks");

        builder.HasKey(bank => bank.Id);

        builder.Property(bank => bank.CanonicalName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(bank => bank.ShortCode)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(bank => bank.OfficialEmail)
            .HasMaxLength(256);

        builder.Property(bank => bank.Notes)
            .HasMaxLength(1024);

        var listComparer = new ValueComparer<IReadOnlyList<GuaranteeDispatchChannel>>(
            (left, right) => left != null && right != null && left.SequenceEqual(right),
            channels => channels.Aggregate(0, (hash, channel) => HashCode.Combine(hash, channel)),
            channels => channels.ToArray());

        builder.Property(bank => bank.SupportedDispatchChannels)
            .HasConversion(
                channels => JsonSerializer.Serialize(channels, JsonOptions),
                value => (IReadOnlyList<GuaranteeDispatchChannel>)(JsonSerializer.Deserialize<List<GuaranteeDispatchChannel>>(value, JsonOptions) ?? new List<GuaranteeDispatchChannel>()))
            .Metadata.SetValueComparer(listComparer);

        builder.Property(bank => bank.SupportedDispatchChannels)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(bank => bank.IsEmailDispatchEnabled)
            .IsRequired();

        builder.Property(bank => bank.IsActive)
            .IsRequired();

        builder.Property(bank => bank.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(bank => bank.ShortCode)
            .IsUnique();
    }
}
