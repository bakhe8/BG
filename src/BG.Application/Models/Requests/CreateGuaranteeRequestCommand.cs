using BG.Domain.Guarantees;

namespace BG.Application.Models.Requests;

public sealed record CreateGuaranteeRequestCommand(
    Guid RequestedByUserId,
    string GuaranteeNumber,
    GuaranteeRequestType RequestType,
    string? RequestedAmount,
    string? RequestedExpiryDate,
    string? Notes);
