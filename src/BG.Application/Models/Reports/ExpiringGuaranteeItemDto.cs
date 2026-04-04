namespace BG.Application.Models.Reports;

public sealed record ExpiringGuaranteeItemDto(
    string GuaranteeNumber,
    string BankName,
    string BeneficiaryName,
    decimal CurrentAmount,
    string CurrencyCode,
    DateOnly ExpiryDate,
    int DaysUntilExpiry);
