namespace BG.Application.Models.Workflow;

public sealed record MoveWorkflowStageCommand(
    Guid DefinitionId,
    Guid StageId,
    WorkflowStageMoveDirection Direction);
