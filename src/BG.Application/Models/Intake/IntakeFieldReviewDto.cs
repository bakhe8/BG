namespace BG.Application.Models.Intake;

public sealed record IntakeFieldReviewDto(
    string FieldKey,
    string LabelResourceKey,
    string Value,
    int ConfidencePercent,
    bool RequiresExplicitReview,
    string? ProvenanceResourceKey = null,
    bool IsExpectedByDocumentForm = false,
    string? RawValue = null,
    bool SourcesAgreed = false,
    string? SourcesConflictDetail = null,
    string? ReviewReason = null);
