using BG.Domain.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

public sealed class RequestWorkflowDefinitionConfiguration : IEntityTypeConfiguration<RequestWorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<RequestWorkflowDefinition> builder)
    {
        builder.ToTable("request_workflow_definitions");

        builder.HasKey(definition => definition.Id);

        builder.Property(definition => definition.Key)
            .HasMaxLength(96)
            .IsRequired();

        builder.Property(definition => definition.RequestType)
            .HasConversion<string>()
            .HasMaxLength(48)
            .IsRequired();

        builder.Property(definition => definition.GuaranteeCategory)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(definition => definition.GuaranteeCategoryResourceKey)
            .HasMaxLength(128);

        builder.Property(definition => definition.TitleResourceKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(definition => definition.SummaryResourceKey)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(definition => definition.IsActive)
            .IsRequired();

        builder.Property(definition => definition.FinalSignatureDelegationPolicy)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(definition => definition.DelegationAmountThreshold)
            .HasColumnType("numeric(18,2)");

        builder.Property(definition => definition.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(definition => definition.Key)
            .IsUnique();

        builder.HasMany(definition => definition.Stages)
            .WithOne(stage => stage.WorkflowDefinition)
            .HasForeignKey(stage => stage.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
