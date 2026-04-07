using BG.Application.Common;

namespace BG.Application.Operations;

public sealed class OperationsReviewQueueSnapshotDto
{
    public OperationsReviewQueueSnapshotDto(
        OperationsActorSummaryDto? activeActor,
        IReadOnlyList<OperationsActorSummaryDto> availableActors,
        IReadOnlyList<OperationsReviewItemDto> items,
        IReadOnlyList<OperationsReviewRecentItemDto> recentlyCompletedItems,
        PageInfoDto itemsPage,
        IReadOnlyList<RequestWorkflowTemplateDto> workflowTemplates,
        int openItemsCount,
        int pendingItemsCount,
        int routedItemsCount,
        bool hasEligibleActor,
        string? contextNoticeResourceKey)
    {
        ActiveActor = activeActor;
        AvailableActors = availableActors;
        Items = items;
        RecentlyCompletedItems = recentlyCompletedItems;
        ItemsPage = itemsPage;
        WorkflowTemplates = workflowTemplates;
        OpenItemsCount = openItemsCount;
        PendingItemsCount = pendingItemsCount;
        RoutedItemsCount = routedItemsCount;
        HasEligibleActor = hasEligibleActor;
        ContextNoticeResourceKey = contextNoticeResourceKey;
    }

    public OperationsActorSummaryDto? ActiveActor { get; }
    public IReadOnlyList<OperationsActorSummaryDto> AvailableActors { get; }
    public IReadOnlyList<OperationsReviewItemDto> Items { get; }
    public IReadOnlyList<OperationsReviewRecentItemDto> RecentlyCompletedItems { get; }
    public PageInfoDto ItemsPage { get; }
    public IReadOnlyList<RequestWorkflowTemplateDto> WorkflowTemplates { get; }
    public int OpenItemsCount { get; }
    public int PendingItemsCount { get; }
    public int RoutedItemsCount { get; }
    public bool HasEligibleActor { get; }
    public string? ContextNoticeResourceKey { get; }
}
