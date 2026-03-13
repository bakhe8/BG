namespace BG.Application.Intake;

internal sealed record IntakeVerifiedDataSnapshot(
    string? ScenarioKey,
    string? DocumentFormKey,
    string? GuaranteeNumber,
    string? BankName,
    string? Amount,
    string? NewExpiryDate,
    string? StatusStatement);
