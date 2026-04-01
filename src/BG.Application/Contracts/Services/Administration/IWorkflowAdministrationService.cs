using BG.Application.Common;
using BG.Application.Models.Workflow;

namespace BG.Application.Contracts.Services;

public interface IWorkflowAdministrationService
{
    Task<WorkflowAdministrationSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<Guid>> UpdateGovernanceAsync(UpdateWorkflowGovernanceCommand command, CancellationToken cancellationToken = default);

    Task<OperationResult<Guid>> AddStageAsync(AddWorkflowStageCommand command, CancellationToken cancellationToken = default);

    Task<OperationResult<Guid>> UpdateStageAsync(UpdateWorkflowStageCommand command, CancellationToken cancellationToken = default);

    Task<OperationResult<Guid>> MoveStageAsync(MoveWorkflowStageCommand command, CancellationToken cancellationToken = default);

    Task<OperationResult<Guid>> RemoveStageAsync(RemoveWorkflowStageCommand command, CancellationToken cancellationToken = default);
}
