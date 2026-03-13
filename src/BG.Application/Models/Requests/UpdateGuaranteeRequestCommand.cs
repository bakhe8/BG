namespace BG.Application.Models.Requests;

public sealed record UpdateGuaranteeRequestCommand(
    Guid RequestedByUserId,
    Guid RequestId,
    string? RequestedAmount,
    string? RequestedExpiryDate,
    string? Notes);
