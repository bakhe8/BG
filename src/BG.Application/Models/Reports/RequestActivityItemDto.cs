using BG.Domain.Guarantees;

namespace BG.Application.Models.Reports;

public sealed record RequestActivityItemDto(
    Guid RequestId,
    string GuaranteeNumber,
    GuaranteeRequestType RequestType,
    GuaranteeRequestStatus Status,
    string RequestedByDisplayName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);
