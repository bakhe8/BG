using BG.Application.Common;
using BG.Application.Models.Dispatch;

namespace BG.Application.Contracts.Services;

public interface IDispatchWorkspaceService
{
    Task<DispatchWorkspaceSnapshotDto> GetWorkspaceAsync(
        Guid? dispatcherActorId,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);

    Task<OperationResult<PrintDispatchLetterReceiptDto>> PrintDispatchLetterAsync(
        PrintDispatchLetterCommand command,
        CancellationToken cancellationToken = default);

    Task<OperationResult<RecordDispatchReceiptDto>> RecordDispatchAsync(
        RecordDispatchCommand command,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ConfirmDispatchDeliveryReceiptDto>> ConfirmDeliveryAsync(
        ConfirmDispatchDeliveryCommand command,
        CancellationToken cancellationToken = default);
}
