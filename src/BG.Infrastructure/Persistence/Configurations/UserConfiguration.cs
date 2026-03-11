using BG.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Username)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(user => user.NormalizedUsername)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(user => user.DisplayName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(256);

        builder.Property(user => user.ExternalId)
            .HasMaxLength(128);

        builder.Property(user => user.SourceType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(user => user.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(user => user.NormalizedUsername)
            .IsUnique();

        builder.HasMany(user => user.UserRoles)
            .WithOne(userRole => userRole.User)
            .HasForeignKey(userRole => userRole.UserId);
    }
}
