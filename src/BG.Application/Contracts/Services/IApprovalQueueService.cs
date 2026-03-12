using BG.Application.Common;
using BG.Application.Models.Approvals;

namespace BG.Application.Contracts.Services;

public interface IApprovalQueueService
{
    Task<ApprovalQueueSnapshotDto> GetWorkspaceAsync(
        Guid? approverActorId,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);

    Task<ApprovalRequestDossierSnapshotDto> GetDossierAsync(
        Guid? approverActorId,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ApprovalDecisionReceiptDto>> ApproveAsync(ApprovalDecisionCommand command, CancellationToken cancellationToken = default);

    Task<OperationResult<ApprovalDecisionReceiptDto>> ReturnAsync(ApprovalDecisionCommand command, CancellationToken cancellationToken = default);

    Task<OperationResult<ApprovalDecisionReceiptDto>> RejectAsync(ApprovalDecisionCommand command, CancellationToken cancellationToken = default);
}
