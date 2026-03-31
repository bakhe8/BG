namespace BG.Application.ReferenceData;

internal sealed record GuaranteeDocumentBankProfileDefinition(
    string Key,
    string BankResourceKey,
    string CanonicalBankName,
    string ReferencePrefix,
    IReadOnlyList<string> FileNameHints,
    IReadOnlyList<string> BankNameHints);
