namespace BG.Application.Models.Intake;

public sealed record OcrDocumentFieldCandidateDto(
    string FieldKey,
    string Value,
    int ConfidencePercent,
    int PageNumber,
    string? BoundingBox,
    string? SourceLabel);
