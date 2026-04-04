namespace BG.Application.Operations;

public sealed record ReturnBankResponseCommand(
    Guid OperationsActorUserId,
    Guid ReviewItemId);
