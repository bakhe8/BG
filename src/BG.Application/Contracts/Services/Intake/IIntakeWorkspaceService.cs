using BG.Application.Models.Intake;

namespace BG.Application.Contracts.Services;

public interface IIntakeWorkspaceService
{
    Task<IntakeWorkspaceSnapshotDto> GetWorkspaceAsync(
        Guid? intakeActorId = null,
        string? scenarioKey = null,
        CancellationToken cancellationToken = default);
}
