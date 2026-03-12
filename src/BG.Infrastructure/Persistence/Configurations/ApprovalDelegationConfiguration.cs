using BG.Domain.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

public sealed class ApprovalDelegationConfiguration : IEntityTypeConfiguration<ApprovalDelegation>
{
    public void Configure(EntityTypeBuilder<ApprovalDelegation> builder)
    {
        builder.ToTable("approval_delegations");

        builder.HasKey(delegation => delegation.Id);

        builder.Property(delegation => delegation.Reason)
            .HasMaxLength(512);

        builder.Property(delegation => delegation.RevocationReason)
            .HasMaxLength(512);

        builder.HasOne(delegation => delegation.DelegatorUser)
            .WithMany()
            .HasForeignKey(delegation => delegation.DelegatorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(delegation => delegation.DelegateUser)
            .WithMany()
            .HasForeignKey(delegation => delegation.DelegateUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(delegation => delegation.Role)
            .WithMany()
            .HasForeignKey(delegation => delegation.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(delegation => new { delegation.DelegateUserId, delegation.RoleId, delegation.StartsAtUtc, delegation.EndsAtUtc });
    }
}
