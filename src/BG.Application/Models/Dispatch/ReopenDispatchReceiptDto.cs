namespace BG.Application.Models.Dispatch;

public sealed record ReopenDispatchReceiptDto(
    Guid RequestId,
    string GuaranteeNumber,
    string ReferenceNumber);
