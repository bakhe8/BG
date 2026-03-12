using BG.Domain.Guarantees;

namespace BG.Application.Models.Workflow;

public sealed record WorkflowDefinitionAdminDto(
    Guid Id,
    string Key,
    GuaranteeRequestType RequestType,
    GuaranteeCategory? GuaranteeCategory,
    string? GuaranteeCategoryResourceKey,
    string TitleResourceKey,
    string SummaryResourceKey,
    bool IsActive,
    IReadOnlyList<string> IntegrityIssueResourceKeys,
    IReadOnlyList<WorkflowStageAdminDto> Stages,
    BG.Domain.Workflow.ApprovalDelegationPolicy FinalSignatureDelegationPolicy = BG.Domain.Workflow.ApprovalDelegationPolicy.Inherit,
    decimal? DelegationAmountThreshold = null);
