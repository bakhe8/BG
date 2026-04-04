using BG.Application.Common;
using BG.Application.Models.Dispatch;

namespace BG.Application.Contracts.Services;

public interface IDispatchWorkspaceService
{
    Task<OperationResult<DispatchLetterPdfResult>> GetLetterPdfAsync(
        Guid dispatcherUserId,
        Guid requestId,
        string referenceNumber,
        DateOnly letterDate,
        CancellationToken cancellationToken = default);

    Task<DispatchWorkspaceSnapshotDto> GetWorkspaceAsync(
        Guid? dispatcherActorId,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);

    Task<OperationResult<DispatchLetterPreviewDto>> GetLetterPreviewAsync(
        Guid dispatcherUserId,
        Guid requestId,
        string? referenceNumber,
        string? letterDate,
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

    Task<OperationResult<ReopenDispatchReceiptDto>> ReopenDispatchAsync(
        ReopenDispatchCommand command,
        CancellationToken cancellationToken = default);
}
