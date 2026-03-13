namespace BG.Application.Models.Requests;

public sealed record WithdrawGuaranteeRequestReceiptDto(
    Guid RequestId,
    string GuaranteeNumber);
