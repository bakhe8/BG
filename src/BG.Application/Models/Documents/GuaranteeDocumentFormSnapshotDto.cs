namespace BG.Application.Models.Documents;

public sealed record GuaranteeDocumentFormSnapshotDto(
    string Key,
    string BankResourceKey,
    string TitleResourceKey,
    string SummaryResourceKey);
