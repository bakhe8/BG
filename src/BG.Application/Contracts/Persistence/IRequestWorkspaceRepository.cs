using BG.Application.Common;
using BG.Application.Models.Requests;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Workflow;

namespace BG.Application.Contracts.Persistence;

public interface IRequestWorkspaceRepository
{
    Task<IReadOnlyList<User>> ListRequestActorsAsync(CancellationToken cancellationToken = default);

    Task<User?> GetRequestActorByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PagedResult<RequestListItemReadModel>> ListOwnedRequestsAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Guarantee?> GetGuaranteeByNumberAsync(string guaranteeNumber, CancellationToken cancellationToken = default);

    Task<GuaranteeRequest?> GetOwnedRequestByIdAsync(Guid requestId, Guid userId, CancellationToken cancellationToken = default);

    void TrackCreatedRequestGraph(GuaranteeRequest request);

    void TrackNewApprovalProcessGraph(RequestApprovalProcess approvalProcess);

    void TrackLedgerEvents(IEnumerable<GuaranteeEvent> ledgerEvents);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
