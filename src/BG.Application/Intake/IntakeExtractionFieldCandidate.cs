namespace BG.Application.Intake;

internal sealed record IntakeExtractionFieldCandidate(
    string FieldKey,
    string Value,
    string RawValue,
    IntakeFieldValueSource Source,
    int ConfidencePercent,
    bool IsValid = true,
    string? ValidationMessage = null);
