using BG.Application.Common;
using BG.Application.Models.Requests;
using BG.Application.Operations;
using BG.Domain.Guarantees;

namespace BG.Application.Contracts.Services;

public interface IRequestWorkspaceService
{
    Task<RequestWorkspaceSnapshotDto> GetWorkspaceAsync(
        Guid? requestedActorId,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);

    Task<RequestWorkflowTemplateDto?> GetWorkflowTemplateAsync(
        string? guaranteeNumber,
        GuaranteeRequestType requestType,
        CancellationToken cancellationToken = default);

    Task<OperationResult<SubmitGuaranteeRequestReceiptDto>> SubmitRequestForApprovalAsync(
        Guid requestedByUserId,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<CreateGuaranteeRequestReceiptDto>> CreateRequestAsync(
        CreateGuaranteeRequestCommand command,
        CancellationToken cancellationToken = default);
}
