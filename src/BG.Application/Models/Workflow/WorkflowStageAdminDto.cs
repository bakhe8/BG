namespace BG.Application.Models.Workflow;

public sealed record WorkflowStageAdminDto(
    Guid Id,
    int Sequence,
    Guid? RoleId,
    string? RoleName,
    string? TitleResourceKey,
    string? SummaryResourceKey,
    string? CustomTitle,
    string? CustomSummary,
    bool RequiresLetterSignature = true,
    BG.Domain.Workflow.ApprovalDelegationPolicy DelegationPolicy = BG.Domain.Workflow.ApprovalDelegationPolicy.Inherit);
