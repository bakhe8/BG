namespace BG.Application.Operations;

public sealed record ReopenAppliedBankResponseReceiptDto(
    Guid ReviewItemId,
    Guid RequestId,
    string GuaranteeNumber);
