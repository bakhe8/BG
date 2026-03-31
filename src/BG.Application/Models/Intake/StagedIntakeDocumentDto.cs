namespace BG.Application.Models.Intake;

public sealed record StagedIntakeDocumentDto(
    string Token,
    string OriginalFileName,
    long FileSize,
    string? StagedFilePath = null);
