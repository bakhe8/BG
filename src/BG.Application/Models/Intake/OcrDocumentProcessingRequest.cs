namespace BG.Application.Models.Intake;

public sealed record OcrDocumentProcessingRequest(
    string StagedDocumentToken,
    string FilePath,
    string OriginalFileName,
    string ScenarioKey,
    string DocumentFormKey,
    string BankProfileKey,
    string StructuralClassKey,
    string? CanonicalBankName,
    string? ReferencePrefix,
    IReadOnlyList<int>? SelectedPages = null);
