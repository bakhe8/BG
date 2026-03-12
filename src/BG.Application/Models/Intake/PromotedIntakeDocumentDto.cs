namespace BG.Application.Models.Intake;

public sealed record PromotedIntakeDocumentDto(
    string OriginalFileName,
    string StoragePath,
    long FileSize);
