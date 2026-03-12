using BG.Application.Common;

namespace BG.Application.Models.Dispatch;

public sealed record DispatchWorkspaceSnapshotDto(
    DispatchActorSummaryDto? ActiveActor,
    IReadOnlyList<DispatchActorSummaryDto> AvailableActors,
    IReadOnlyList<DispatchQueueItemDto> Items,
    IReadOnlyList<DispatchPendingDeliveryItemDto> PendingDeliveryItems,
    PageInfoDto ItemsPage,
    bool HasEligibleActor,
    string? ContextNoticeResourceKey);
