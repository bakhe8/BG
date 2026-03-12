using BG.Domain.Guarantees;

namespace BG.Application.Operations;

public sealed record RequestWorkflowTemplateDto(
    string Key,
    GuaranteeRequestType RequestType,
    GuaranteeCategory? GuaranteeCategory,
    string? GuaranteeCategoryResourceKey,
    string TitleResourceKey,
    string SummaryResourceKey,
    IReadOnlyList<RequestWorkflowStageTemplateDto> Stages,
    BG.Domain.Workflow.ApprovalDelegationPolicy FinalSignatureDelegationPolicy = BG.Domain.Workflow.ApprovalDelegationPolicy.Inherit,
    decimal? DelegationAmountThreshold = null);
