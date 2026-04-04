namespace BG.Application.Operations;

public sealed record RejectBankResponseCommand(
    Guid OperationsActorUserId,
    Guid ReviewItemId);
