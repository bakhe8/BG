namespace BG.Application.Models.Workflow;

public sealed record UpdateWorkflowGovernanceCommand(
    Guid DefinitionId,
    BG.Domain.Workflow.ApprovalDelegationPolicy FinalSignatureDelegationPolicy = BG.Domain.Workflow.ApprovalDelegationPolicy.Inherit,
    string? DelegationAmountThreshold = null);
