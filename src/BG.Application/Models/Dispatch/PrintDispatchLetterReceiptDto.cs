namespace BG.Application.Models.Dispatch;

public sealed record PrintDispatchLetterReceiptDto(
    Guid RequestId,
    string GuaranteeNumber,
    string ReferenceNumber,
    int PrintCount);
