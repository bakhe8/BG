using BG.Domain.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

public sealed class RequestApprovalStageConfiguration : IEntityTypeConfiguration<RequestApprovalStage>
{
    public void Configure(EntityTypeBuilder<RequestApprovalStage> builder)
    {
        builder.ToTable("request_approval_stages");

        builder.HasKey(stage => stage.Id);

        builder.Property(stage => stage.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(stage => stage.TitleResourceKey)
            .HasMaxLength(128);

        builder.Property(stage => stage.SummaryResourceKey)
            .HasMaxLength(256);

        builder.Property(stage => stage.TitleText)
            .HasMaxLength(128);

        builder.Property(stage => stage.SummaryText)
            .HasMaxLength(256);

        builder.Property(stage => stage.DecisionNote)
            .HasMaxLength(512);

        builder.Property(stage => stage.DelegationPolicy)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasOne(stage => stage.Role)
            .WithMany()
            .HasForeignKey(stage => stage.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(stage => stage.ActedByUser)
            .WithMany()
            .HasForeignKey(stage => stage.ActedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(stage => stage.ActedOnBehalfOfUser)
            .WithMany()
            .HasForeignKey(stage => stage.ActedOnBehalfOfUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(stage => stage.ApprovalDelegation)
            .WithMany()
            .HasForeignKey(stage => stage.ApprovalDelegationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(stage => new { stage.ApprovalProcessId, stage.Sequence })
            .IsUnique();
    }
}
