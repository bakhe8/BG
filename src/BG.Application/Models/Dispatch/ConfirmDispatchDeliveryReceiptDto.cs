namespace BG.Application.Models.Dispatch;

public sealed record ConfirmDispatchDeliveryReceiptDto(
    Guid RequestId,
    string GuaranteeNumber,
    string ReferenceNumber);
