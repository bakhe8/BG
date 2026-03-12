using BG.Application.Common;
using BG.Application.Models.Dispatch;
using BG.Domain.Guarantees;
using BG.Domain.Identity;

namespace BG.Application.Contracts.Persistence;

public interface IDispatchWorkspaceRepository
{
    Task<IReadOnlyList<User>> ListDispatchActorsAsync(CancellationToken cancellationToken = default);

    Task<User?> GetDispatchActorByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PagedResult<DispatchQueueItemReadModel>> ListReadyRequestsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DispatchPendingDeliveryItemReadModel>> ListPendingDeliveryAsync(
        CancellationToken cancellationToken = default);

    Task<GuaranteeRequest?> GetRequestForDispatchAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
