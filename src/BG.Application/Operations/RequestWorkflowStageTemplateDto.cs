namespace BG.Application.Operations;

public sealed record RequestWorkflowStageTemplateDto(
    int Level,
    string? TitleResourceKey,
    string? SummaryResourceKey,
    bool RequiresLetterSignature,
    string CurrentSignatureModeResourceKey,
    string FinalPdfEffectResourceKey,
    BG.Domain.Workflow.ApprovalDelegationPolicy DelegationPolicy = BG.Domain.Workflow.ApprovalDelegationPolicy.Inherit,
    Guid? AssignedRoleId = null,
    string? AssignedRoleName = null,
    string? TitleText = null,
    string? SummaryText = null);
