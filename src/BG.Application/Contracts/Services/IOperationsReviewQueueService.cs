using BG.Application.Common;
using BG.Application.Operations;

namespace BG.Application.Contracts.Services;

public interface IOperationsReviewQueueService
{
    Task<OperationsReviewQueueSnapshotDto> GetSnapshotAsync(
        Guid? operationsActorId,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);

    Task<OperationsReviewItemDto?> GetCompletedItemAsync(
        Guid reviewItemId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ApplyBankResponseReceiptDto>> ApplyBankResponseAsync(
        ApplyBankResponseCommand command,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ReopenAppliedBankResponseReceiptDto>> ReopenAppliedBankResponseAsync(
        ReopenAppliedBankResponseCommand command,
        CancellationToken cancellationToken = default);
}
