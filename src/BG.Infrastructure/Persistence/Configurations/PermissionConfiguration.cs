using BG.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(permission => permission.Key);

        builder.Property(permission => permission.Key)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(permission => permission.Area)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasMany(permission => permission.RolePermissions)
            .WithOne(rolePermission => rolePermission.Permission)
            .HasForeignKey(rolePermission => rolePermission.PermissionKey);
    }
}
