using BG.Application.Common;

namespace BG.Application.Operations;

public sealed record OperationsReviewQueueSnapshotDto(
    OperationsActorSummaryDto? ActiveActor,
    IReadOnlyList<OperationsActorSummaryDto> AvailableActors,
    IReadOnlyList<OperationsReviewItemDto> Items,
    IReadOnlyList<OperationsReviewRecentItemDto> RecentlyCompletedItems,
    PageInfoDto ItemsPage,
    IReadOnlyList<RequestWorkflowTemplateDto> WorkflowTemplates,
    int OpenItemsCount,
    int PendingItemsCount,
    int RoutedItemsCount,
    bool HasEligibleActor,
    string? ContextNoticeResourceKey);
