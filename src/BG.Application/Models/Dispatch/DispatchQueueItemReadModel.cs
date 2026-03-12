using BG.Domain.Guarantees;

namespace BG.Application.Models.Dispatch;

public sealed record DispatchQueueItemReadModel(
    Guid RequestId,
    string GuaranteeNumber,
    GuaranteeCategory GuaranteeCategory,
    GuaranteeRequestType RequestType,
    GuaranteeRequestStatus Status,
    string RequesterDisplayName,
    DateTimeOffset ReadyAtUtc,
    Guid? OutgoingCorrespondenceId,
    string? OutgoingReferenceNumber,
    DateOnly? OutgoingLetterDate,
    int PrintCount,
    DateTimeOffset? LastPrintedAtUtc,
    GuaranteeOutgoingLetterPrintMode? LastPrintMode);
