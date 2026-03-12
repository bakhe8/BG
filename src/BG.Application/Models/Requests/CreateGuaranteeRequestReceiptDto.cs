namespace BG.Application.Models.Requests;

public sealed record CreateGuaranteeRequestReceiptDto(
    Guid RequestId,
    string GuaranteeNumber,
    string RequestTypeResourceKey);
