namespace BG.Application.Models.Intake;

public sealed record OcrFieldFeedbackEntryDto(
    string DocumentToken,
    string ScenarioKey,
    string FieldKey,
    string? DetectedBankName,
    string OriginalValue,
    string CorrectedValue,
    string Source,
    int OriginalConfidencePercent);
