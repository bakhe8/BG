namespace BG.Application.Models.Intake;

public sealed record IntakeFieldSampleDto(
    string LabelResourceKey,
    string Value,
    int ConfidencePercent,
    bool RequiresExplicitReview);
