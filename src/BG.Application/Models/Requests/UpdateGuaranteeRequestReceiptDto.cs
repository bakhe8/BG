namespace BG.Application.Models.Requests;

public sealed record UpdateGuaranteeRequestReceiptDto(
    Guid RequestId,
    string GuaranteeNumber);
