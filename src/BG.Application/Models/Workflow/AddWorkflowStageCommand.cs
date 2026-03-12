namespace BG.Application.Models.Workflow;

public sealed record AddWorkflowStageCommand(
    Guid DefinitionId,
    Guid RoleId,
    string? CustomTitle,
    string? CustomSummary,
    bool? RequiresLetterSignature = null,
    BG.Domain.Workflow.ApprovalDelegationPolicy DelegationPolicy = BG.Domain.Workflow.ApprovalDelegationPolicy.Inherit);
