using BG.Application.Common;

namespace BG.Application.Operations;

public sealed record OperationsReviewQueuePageReadModel(
    PagedResult<OperationsReviewQueueItemReadModel> Items,
    OperationsReviewQueueCountsReadModel Counts);
