namespace BG.Application.Models.Workflow;

public sealed record RemoveWorkflowStageCommand(
    Guid DefinitionId,
    Guid StageId);
