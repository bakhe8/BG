namespace BG.Application.Models.Dispatch;

public sealed record DispatchQueueItemDto(
    Guid RequestId,
    string GuaranteeNumber,
    string GuaranteeCategoryResourceKey,
    string RequestTypeResourceKey,
    string StatusResourceKey,
    string RequesterDisplayName,
    DateTimeOffset ReadyAtUtc,
    Guid? OutgoingCorrespondenceId,
    string? OutgoingReferenceNumber,
    DateOnly? OutgoingLetterDate,
    int PrintCount,
    DateTimeOffset? LastPrintedAtUtc,
    string? LastPrintModeResourceKey);
