namespace BG.Application.Intake;

internal sealed record IntakeExtractionFieldCandidate(
    string FieldKey,
    string Value,
    IntakeFieldValueSource Source);
