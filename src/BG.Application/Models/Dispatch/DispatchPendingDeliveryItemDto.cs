using BG.Application.Models.Documents;

namespace BG.Application.Models.Dispatch;

public sealed record DispatchPendingDeliveryItemDto(
    Guid RequestId,
    Guid CorrespondenceId,
    string GuaranteeNumber,
    string GuaranteeCategoryResourceKey,
    string RequestTypeResourceKey,
    string RequesterDisplayName,
    string ReferenceNumber,
    DateOnly LetterDate,
    GuaranteeDocumentFormSnapshotDto? SourceDocumentForm,
    string DispatchChannelResourceKey,
    string? DispatchReference,
    DateTimeOffset DispatchedAtUtc);
