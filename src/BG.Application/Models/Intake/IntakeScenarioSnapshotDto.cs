namespace BG.Application.Models.Intake;

public sealed record IntakeScenarioSnapshotDto(
    string Key,
    string TitleResourceKey,
    string SummaryResourceKey,
    string SaveOutcomeResourceKey,
    string HandoffResourceKey,
    IReadOnlyList<string> RequiredReviewFieldKeys,
    IReadOnlyList<IntakeFieldReviewDto> SampleFields,
    bool RequiresExistingGuarantee,
    bool RequiresConfirmedExpiryDate,
    bool RequiresConfirmedAmount,
    bool RequiresStatusStatement,
    bool RequiresAttachmentNote);
