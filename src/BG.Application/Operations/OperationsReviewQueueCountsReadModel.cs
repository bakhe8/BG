namespace BG.Application.Operations;

public sealed record OperationsReviewQueueCountsReadModel(
    int OpenItemsCount,
    int PendingItemsCount,
    int RoutedItemsCount);
