namespace BG.Application.Operations;

public sealed record DismissBankResponseReceiptDto(
    Guid ReviewItemId,
    string GuaranteeNumber);
