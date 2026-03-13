namespace BG.Application.Models.Requests;

public sealed record CancelGuaranteeRequestReceiptDto(
    Guid RequestId,
    string GuaranteeNumber);
