namespace BG.Application.Operations;

public sealed record ApplyBankResponseReceiptDto(
    Guid ReviewItemId,
    Guid RequestId,
    string GuaranteeNumber);
