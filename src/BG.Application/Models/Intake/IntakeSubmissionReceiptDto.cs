namespace BG.Application.Models.Intake;

public sealed record IntakeSubmissionReceiptDto(
    Guid GuaranteeId,
    string GuaranteeNumber,
    Guid DocumentId,
    string ScenarioKey,
    string HandoffResourceKey);
