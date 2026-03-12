using BG.Domain.Guarantees;

namespace BG.Application.Models.Requests;

public sealed record RequestListItemReadModel(
    Guid Id,
    string GuaranteeNumber,
    GuaranteeCategory GuaranteeCategory,
    GuaranteeRequestType RequestType,
    GuaranteeRequestStatus Status,
    decimal? RequestedAmount,
    DateOnly? RequestedExpiryDate,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    int CorrespondenceCount,
    string? CurrentStageTitleResourceKey,
    string? CurrentStageTitle,
    string? CurrentStageRoleName,
    string? LastDecisionNote,
    IReadOnlyList<RequestLedgerEntryReadModel> LedgerEntries);
