namespace BG.Application.Contracts.Services;

public interface IExecutionActorAccessor
{
    ExecutionActorSnapshot? GetCurrentActor();
}

public sealed record ExecutionActorSnapshot(
    Guid? UserId,
    string? Username,
    string? DisplayName);
