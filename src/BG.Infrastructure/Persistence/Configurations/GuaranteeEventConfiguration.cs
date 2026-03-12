using BG.Domain.Guarantees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

public sealed class GuaranteeEventConfiguration : IEntityTypeConfiguration<GuaranteeEvent>
{
    public void Configure(EntityTypeBuilder<GuaranteeEvent> builder)
    {
        builder.ToTable("guarantee_events");

        builder.HasKey(guaranteeEvent => guaranteeEvent.Id);

        builder.Property(guaranteeEvent => guaranteeEvent.EventType)
            .HasConversion<string>()
            .HasMaxLength(48)
            .IsRequired();

        builder.Property(guaranteeEvent => guaranteeEvent.Summary)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(guaranteeEvent => guaranteeEvent.ActorDisplayName)
            .HasMaxLength(256);

        builder.Property(guaranteeEvent => guaranteeEvent.ApprovalStageLabel)
            .HasMaxLength(128);

        builder.Property(guaranteeEvent => guaranteeEvent.ApprovalPolicyResourceKey)
            .HasMaxLength(128);

        builder.Property(guaranteeEvent => guaranteeEvent.ApprovalResponsibleSignerDisplayName)
            .HasMaxLength(256);

        builder.Property(guaranteeEvent => guaranteeEvent.ApprovalExecutionMode)
            .HasMaxLength(32);

        builder.Property(guaranteeEvent => guaranteeEvent.DispatchStageResourceKey)
            .HasMaxLength(128);

        builder.Property(guaranteeEvent => guaranteeEvent.DispatchMethodResourceKey)
            .HasMaxLength(128);

        builder.Property(guaranteeEvent => guaranteeEvent.DispatchPolicyResourceKey)
            .HasMaxLength(128);

        builder.Property(guaranteeEvent => guaranteeEvent.OperationsScenarioTitleResourceKey)
            .HasMaxLength(128);

        builder.Property(guaranteeEvent => guaranteeEvent.OperationsLaneResourceKey)
            .HasMaxLength(128);

        builder.Property(guaranteeEvent => guaranteeEvent.OperationsMatchConfidenceResourceKey)
            .HasMaxLength(128);

        builder.Property(guaranteeEvent => guaranteeEvent.OperationsPolicyResourceKey)
            .HasMaxLength(128);

        builder.Property(guaranteeEvent => guaranteeEvent.PreviousAmount)
            .HasPrecision(18, 2);

        builder.Property(guaranteeEvent => guaranteeEvent.NewAmount)
            .HasPrecision(18, 2);

        builder.Property(guaranteeEvent => guaranteeEvent.PreviousStatus)
            .HasMaxLength(32);

        builder.Property(guaranteeEvent => guaranteeEvent.NewStatus)
            .HasMaxLength(32);

        builder.HasOne(guaranteeEvent => guaranteeEvent.GuaranteeRequest)
            .WithMany()
            .HasForeignKey(guaranteeEvent => guaranteeEvent.GuaranteeRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(guaranteeEvent => guaranteeEvent.GuaranteeCorrespondence)
            .WithMany()
            .HasForeignKey(guaranteeEvent => guaranteeEvent.GuaranteeCorrespondenceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(guaranteeEvent => guaranteeEvent.GuaranteeDocument)
            .WithMany()
            .HasForeignKey(guaranteeEvent => guaranteeEvent.GuaranteeDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(guaranteeEvent => guaranteeEvent.ActorUser)
            .WithMany()
            .HasForeignKey(guaranteeEvent => guaranteeEvent.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(guaranteeEvent => new { guaranteeEvent.GuaranteeId, guaranteeEvent.OccurredAtUtc });
    }
}
