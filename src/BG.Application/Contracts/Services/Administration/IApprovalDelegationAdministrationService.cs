using BG.Application.Common;
using BG.Application.Models.Approvals;

namespace BG.Application.Contracts.Services;

public interface IApprovalDelegationAdministrationService
{
    Task<ApprovalDelegationAdministrationSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<Guid>> CreateAsync(CreateApprovalDelegationCommand command, CancellationToken cancellationToken = default);

    Task<OperationResult<Guid>> RevokeAsync(RevokeApprovalDelegationCommand command, CancellationToken cancellationToken = default);
}
