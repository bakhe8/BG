using BG.Domain.Guarantees;

namespace BG.Application.ReferenceData;

public sealed record GuaranteeDocumentFormDefinition(
    string Key,
    GuaranteeDocumentType DocumentType,
    string BankResourceKey,
    string TitleResourceKey,
    string SummaryResourceKey,
    string? CanonicalBankName,
    IReadOnlyList<string> ScenarioKeys,
    IReadOnlyList<string> ExpectedFieldKeys,
    IReadOnlyList<string> ExpectedCueResourceKeys,
    IReadOnlyList<string> FileNameHints,
    IReadOnlyList<string> BankNameHints);
