namespace BG.Application.Models.Workflow;

public sealed record WorkflowAdministrationSnapshotDto(
    IReadOnlyList<WorkflowDefinitionAdminDto> Definitions,
    IReadOnlyList<WorkflowRoleOptionDto> AvailableRoles);
