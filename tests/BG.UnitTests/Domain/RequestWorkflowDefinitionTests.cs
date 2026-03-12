using BG.Domain.Guarantees;
using BG.Domain.Workflow;

namespace BG.UnitTests.Domain;

public sealed class RequestWorkflowDefinitionTests
{
    [Fact]
    public void Definition_with_unassigned_stage_is_not_operationally_ready()
    {
        var definition = CreateDefinition();

        definition.AddStage(
            roleId: null,
            titleResourceKey: "WorkflowStage_GuaranteesSupervisor_Title",
            summaryResourceKey: "WorkflowStage_GuaranteesSupervisor_Summary",
            customTitle: null,
            customSummary: null,
            requiresLetterSignature: true,
            modifiedAtUtc: DateTimeOffset.UtcNow);

        Assert.False(definition.IsActive);
        Assert.False(definition.IsOperationallyReady);
        Assert.Contains("workflow.role_required", definition.GetIntegrityIssueResourceKeys());
    }

    [Fact]
    public void Updating_last_unassigned_stage_to_role_activates_definition()
    {
        var definition = CreateDefinition();
        definition.AddStage(
            roleId: null,
            titleResourceKey: "WorkflowStage_GuaranteesSupervisor_Title",
            summaryResourceKey: "WorkflowStage_GuaranteesSupervisor_Summary",
            customTitle: null,
            customSummary: null,
            requiresLetterSignature: true,
            modifiedAtUtc: DateTimeOffset.UtcNow);
        var stage = Assert.Single(definition.Stages);

        definition.UpdateStage(stage.Id, Guid.NewGuid(), null, null, DateTimeOffset.UtcNow);

        Assert.True(definition.IsActive);
        Assert.True(definition.IsOperationallyReady);
        Assert.Empty(definition.GetIntegrityIssueResourceKeys());
    }

    [Fact]
    public void Removing_last_stage_is_not_allowed()
    {
        var definition = CreateDefinition();
        definition.AddStage(
            roleId: Guid.NewGuid(),
            titleResourceKey: "WorkflowStage_GuaranteesSupervisor_Title",
            summaryResourceKey: "WorkflowStage_GuaranteesSupervisor_Summary",
            customTitle: null,
            customSummary: null,
            requiresLetterSignature: true,
            modifiedAtUtc: DateTimeOffset.UtcNow);
        var stage = Assert.Single(definition.Stages);

        var exception = Assert.Throws<InvalidOperationException>(() => definition.RemoveStage(stage.Id, DateTimeOffset.UtcNow));

        Assert.Contains("at least one stage", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static RequestWorkflowDefinition CreateDefinition()
    {
        return new RequestWorkflowDefinition(
            "Extend",
            GuaranteeRequestType.Extend,
            guaranteeCategory: null,
            guaranteeCategoryResourceKey: null,
            "WorkflowTemplate_Extend_Title",
            "WorkflowTemplate_Extend_Summary",
            DateTimeOffset.UtcNow);
    }
}
