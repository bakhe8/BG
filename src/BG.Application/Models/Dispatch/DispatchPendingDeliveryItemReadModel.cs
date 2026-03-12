using BG.Domain.Guarantees;

namespace BG.Application.Models.Dispatch;

public sealed record DispatchPendingDeliveryItemReadModel(
    Guid RequestId,
    Guid CorrespondenceId,
    string GuaranteeNumber,
    GuaranteeCategory GuaranteeCategory,
    GuaranteeRequestType RequestType,
    string RequesterDisplayName,
    string ReferenceNumber,
    DateOnly LetterDate,
    GuaranteeDispatchChannel DispatchChannel,
    string? DispatchReference,
    DateTimeOffset DispatchedAtUtc);
