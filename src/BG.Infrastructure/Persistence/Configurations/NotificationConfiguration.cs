using BG.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .ValueGeneratedNever();

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(n => n.Link)
            .HasMaxLength(2048);

        builder.Property(n => n.RequiredPermission)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(n => n.CreatedAtUtc)
            .IsRequired();

        builder.Property(n => n.TargetUserId);

        builder.Property(n => n.IsRead)
            .IsRequired();

        builder.Property(n => n.ReadAtUtc);

        builder.HasIndex(n => n.RequiredPermission);
        builder.HasIndex(n => n.TargetUserId);
        builder.HasIndex(n => n.CreatedAtUtc);
    }
}
