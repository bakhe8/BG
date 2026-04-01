using BG.Application.Contracts.Services;

namespace BG.Application.Services;

internal sealed class NullExecutionActorAccessor : IExecutionActorAccessor
{
    public ExecutionActorSnapshot? GetCurrentActor()
    {
        return null;
    }
}
