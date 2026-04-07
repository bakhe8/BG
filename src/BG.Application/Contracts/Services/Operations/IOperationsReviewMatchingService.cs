using BG.Application.Operations;

namespace BG.Application.Contracts.Services;

public interface IOperationsReviewMatchingService
{
    IReadOnlyList<OperationsUnifiedSuggestion> SuggestMatches(OperationsReviewQueueItemReadModel item);
}
