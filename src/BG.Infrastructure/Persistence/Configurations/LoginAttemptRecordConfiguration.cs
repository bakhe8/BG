using BG.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

internal sealed class LoginAttemptRecordConfiguration : IEntityTypeConfiguration<LoginAttemptRecord>
{
    public void Configure(EntityTypeBuilder<LoginAttemptRecord> builder)
    {
        builder.ToTable("LoginAttemptRecords");

        builder.HasKey(record => record.TrackingKey);

        builder.Property(record => record.TrackingKey)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(record => record.FailureCount)
            .IsRequired();

        builder.Property(record => record.WindowExpiresAtUtc)
            .IsRequired();

        builder.Property(record => record.LockedUntilUtc);

        builder.Property(record => record.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(record => record.WindowExpiresAtUtc)
            .HasDatabaseName("IX_LoginAttemptRecords_WindowExpiresAtUtc");
    }
}
