namespace BG.Application.Models.Intake;

public sealed record IntakeExtractionDraftDto(
    string ScenarioKey,
    string StagedDocumentToken,
    string OriginalFileName,
    int PageCount,
    string ExtractionRouteResourceKey,
    IReadOnlyList<IntakeFieldReviewDto> Fields);
