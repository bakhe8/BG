using BG.Domain.Guarantees;

namespace BG.Application.Models.Dispatch;

public sealed record DispatchLetterPreviewDto(
    Guid RequestId,
    string GuaranteeNumber,
    string BankName,
    string BeneficiaryName,
    string PrincipalName,
    string CurrencyCode,
    decimal CurrentAmount,
    DateOnly IssueDate,
    DateOnly CurrentExpiryDate,
    GuaranteeRequestType RequestType,
    string RequesterDisplayName,
    string? ReferenceNumber,
    DateOnly LetterDate,
    decimal? RequestedAmount,
    DateOnly? RequestedExpiryDate,
    string? Notes,
    string PreparedByDisplayName,
    DateTimeOffset GeneratedAtUtc,
    bool IsDraftPreview,
    int PrintCount);
