using BG.Domain.Guarantees;

namespace BG.Application.ReferenceData;

public sealed record GuaranteeDocumentFormDefinition(
    string Key,
    GuaranteeDocumentType DocumentType,
    string BankProfileKey,
    string BankResourceKey,
    string TitleResourceKey,
    string SummaryResourceKey,
    string StructuralClassKey,
    string? CanonicalBankName,
    string? ReferencePrefix,
    IReadOnlyList<string> ScenarioKeys,
    IReadOnlyList<string> ExpectedFieldKeys,
    IReadOnlyList<string> ExpectedCueResourceKeys,
    IReadOnlyList<string> FileNameHints,
    IReadOnlyList<string> BankNameHints);
