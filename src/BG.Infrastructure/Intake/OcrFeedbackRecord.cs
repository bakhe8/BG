namespace BG.Infrastructure.Intake;

internal sealed class OcrFeedbackRecord
{
    public int Id { get; set; }
    public string DocumentToken { get; set; } = string.Empty;
    public string ScenarioKey { get; set; } = string.Empty;
    public string FieldKey { get; set; } = string.Empty;
    public string? DetectedBankName { get; set; }
    public string OriginalValue { get; set; } = string.Empty;
    public string CorrectedValue { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public int OriginalConfidencePercent { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
}
