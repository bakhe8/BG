namespace BG.Application.Models.Requests;

public sealed record RequestSummaryDto(
    Guid Id,
    string GuaranteeNumber,
    string GuaranteeCategoryResourceKey,
    string RequestTypeResourceKey,
    string StatusResourceKey,
    string? RequestedAmount,
    string? RequestedExpiryDate,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    int CorrespondenceCount,
    string? CurrentStageTitleResourceKey,
    string? CurrentStageTitle,
    string? CurrentStageRoleName,
    bool CanSubmitForApproval,
    string? LastDecisionResourceKey,
    string? LastDecisionNote,
    IReadOnlyList<RequestLedgerEntryDto> LedgerEntries);
