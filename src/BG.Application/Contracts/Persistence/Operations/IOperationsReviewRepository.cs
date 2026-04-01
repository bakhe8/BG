using BG.Application.Operations;
using BG.Domain.Identity;
using BG.Domain.Operations;

namespace BG.Application.Contracts.Persistence;

public interface IOperationsReviewRepository
{
    Task<IReadOnlyList<User>> ListOperationsActorsAsync(CancellationToken cancellationToken = default);

    Task<User?> GetOperationsActorByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<OperationsReviewQueuePageReadModel> ListOpenAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationsReviewRecentItemReadModel>> ListRecentlyCompletedAsync(
        int takeCount,
        CancellationToken cancellationToken = default);

    Task<OperationsReviewItem?> GetOpenItemByIdAsync(Guid reviewItemId, CancellationToken cancellationToken = default);

    Task<OperationsReviewItem?> GetItemByIdAsync(Guid reviewItemId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
