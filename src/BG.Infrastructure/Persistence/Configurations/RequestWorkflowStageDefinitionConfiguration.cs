using BG.Domain.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

public sealed class RequestWorkflowStageDefinitionConfiguration : IEntityTypeConfiguration<RequestWorkflowStageDefinition>
{
    public void Configure(EntityTypeBuilder<RequestWorkflowStageDefinition> builder)
    {
        builder.ToTable("request_workflow_stages");

        builder.HasKey(stage => stage.Id);

        builder.Property(stage => stage.Sequence)
            .IsRequired();

        builder.Property(stage => stage.TitleResourceKey)
            .HasMaxLength(128);

        builder.Property(stage => stage.SummaryResourceKey)
            .HasMaxLength(256);

        builder.Property(stage => stage.CustomTitle)
            .HasMaxLength(128);

        builder.Property(stage => stage.CustomSummary)
            .HasMaxLength(256);

        builder.Property(stage => stage.RequiresLetterSignature)
            .IsRequired();

        builder.Property(stage => stage.DelegationPolicy)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(stage => new { stage.WorkflowDefinitionId, stage.Sequence })
            .IsUnique();

        builder.HasOne(stage => stage.Role)
            .WithMany()
            .HasForeignKey(stage => stage.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
