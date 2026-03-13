namespace BG.Application.Models.Intake;

public sealed record IntakeDocumentFormOptionDto(
    string Key,
    string BankResourceKey,
    string TitleResourceKey,
    string SummaryResourceKey,
    IReadOnlyList<string> ExpectedFieldKeys,
    IReadOnlyList<string> ExpectedCueResourceKeys);
