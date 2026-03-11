using BG.Application.Models.Intake;

namespace BG.Application.Contracts.Services;

public interface IIntakeWorkspaceService
{
    IntakeWorkspaceSnapshotDto GetWorkspace(string? scenarioKey = null);
}
