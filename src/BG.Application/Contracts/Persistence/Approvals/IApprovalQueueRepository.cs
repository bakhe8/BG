using BG.Application.Common;
using BG.Application.Models.Approvals;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Workflow;

namespace BG.Application.Contracts.Persistence;

public interface IApprovalQueueRepository
{
    Task<IReadOnlyList<User>> ListApprovalActorsAsync(CancellationToken cancellationToken = default);

    Task<User?> GetApprovalActorByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApprovalDelegation>> ListActiveDelegationsAsync(
        Guid delegateUserId,
        DateTimeOffset effectiveAtUtc,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ApprovalQueueItemReadModel>> ListActionableRequestsAsync(
        IEnumerable<Guid> actionableRoleIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ApprovalQueueItemReadModel?> GetActionableRequestAsync(
        Guid requestId,
        IEnumerable<Guid> actionableRoleIds,
        CancellationToken cancellationToken = default);

    Task<GuaranteeRequest?> GetRequestForApprovalAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task<GuaranteeDocument?> GetRequestDocumentAsync(Guid requestId, Guid documentId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
