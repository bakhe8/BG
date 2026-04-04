using BG.Domain.Guarantees;

namespace BG.Application.Models.Reports;

public sealed record GuaranteePortfolioItemDto(
    string GuaranteeNumber,
    string BankName,
    string BeneficiaryName,
    string PrincipalName,
    GuaranteeCategory Category,
    GuaranteeStatus Status,
    decimal CurrentAmount,
    string CurrencyCode,
    DateOnly IssueDate,
    DateOnly ExpiryDate);
