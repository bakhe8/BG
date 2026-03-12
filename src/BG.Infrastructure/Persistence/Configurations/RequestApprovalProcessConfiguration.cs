using BG.Domain.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

public sealed class RequestApprovalProcessConfiguration : IEntityTypeConfiguration<RequestApprovalProcess>
{
    public void Configure(EntityTypeBuilder<RequestApprovalProcess> builder)
    {
        builder.ToTable("request_approval_processes");

        builder.HasKey(process => process.Id);

        builder.Property(process => process.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(process => process.FinalSignatureDelegationPolicy)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(process => process.DelegationAmountThreshold)
            .HasColumnType("numeric(18,2)");

        builder.Property(process => process.LastReturnedNote)
            .HasMaxLength(512);

        builder.Property(process => process.LastRejectedNote)
            .HasMaxLength(512);

        builder.HasOne(process => process.GuaranteeRequest)
            .WithOne(request => request.ApprovalProcess)
            .HasForeignKey<RequestApprovalProcess>(process => process.GuaranteeRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(process => process.WorkflowDefinition)
            .WithMany()
            .HasForeignKey(process => process.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(process => process.Stages)
            .WithOne(stage => stage.ApprovalProcess)
            .HasForeignKey(stage => stage.ApprovalProcessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(process => process.GuaranteeRequestId)
            .IsUnique();
    }
}
