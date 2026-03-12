namespace BG.Application.Models.Workflow;

public sealed record UpdateWorkflowStageCommand(
    Guid DefinitionId,
    Guid StageId,
    Guid? RoleId,
    string? CustomTitle,
    string? CustomSummary,
    bool? RequiresLetterSignature = null,
    BG.Domain.Workflow.ApprovalDelegationPolicy DelegationPolicy = BG.Domain.Workflow.ApprovalDelegationPolicy.Inherit);
