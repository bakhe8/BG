namespace BG.Application.Operations;

public sealed record ApplyBankResponseCommand(
    Guid OperationsActorUserId,
    Guid ReviewItemId,
    Guid RequestId,
    string? ConfirmedExpiryDate,
    string? ConfirmedAmount,
    string? ReplacementGuaranteeNumber,
    string? Note);
