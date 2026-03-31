namespace BG.Application.Operations;

public sealed record ReopenAppliedBankResponseCommand(
    Guid OperationsActorUserId,
    Guid ReviewItemId,
    string? CorrectionNote);
