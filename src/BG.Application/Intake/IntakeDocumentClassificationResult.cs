namespace BG.Application.Intake;

internal sealed record IntakeDocumentClassificationResult(
    IntakeExtractionStrategy Strategy,
    string RouteResourceKey,
    int PageCount);
