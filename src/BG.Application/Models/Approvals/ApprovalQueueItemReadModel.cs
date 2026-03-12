using BG.Domain.Guarantees;
using BG.Domain.Workflow;

namespace BG.Application.Models.Approvals;

public sealed record ApprovalQueueItemReadModel(
    Guid RequestId,
    string GuaranteeNumber,
    GuaranteeCategory GuaranteeCategory,
    GuaranteeRequestType RequestType,
    GuaranteeRequestChannel RequestChannel,
    GuaranteeRequestStatus Status,
    string RequesterDisplayName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset SubmittedAtUtc,
    decimal CurrentGuaranteeAmount,
    decimal? RequestedAmount,
    DateOnly? RequestedExpiryDate,
    string? Notes,
    int TotalStageCount,
    int? CurrentStageSequence,
    Guid? CurrentStageRoleId,
    string? CurrentStageTitleResourceKey,
    string? CurrentStageTitle,
    string? CurrentStageRoleName,
    bool RequiresLetterSignature,
    IReadOnlyList<ApprovalPriorSignatureReadModel> PriorSignatures,
    IReadOnlyList<ApprovalRequestAttachmentReadModel> Attachments,
    IReadOnlyList<ApprovalRequestTimelineEntryReadModel> TimelineEntries,
    ApprovalDelegationPolicy FinalSignatureDelegationPolicy = ApprovalDelegationPolicy.Inherit,
    decimal? DelegationAmountThreshold = null,
    ApprovalDelegationPolicy CurrentStageDelegationPolicy = ApprovalDelegationPolicy.Inherit);
