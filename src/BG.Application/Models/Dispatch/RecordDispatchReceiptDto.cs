namespace BG.Application.Models.Dispatch;

public sealed record RecordDispatchReceiptDto(
    Guid RequestId,
    string GuaranteeNumber,
    string ReferenceNumber);
