using BG.Application.Models.Intake;
using BG.Domain.Guarantees;
using BG.Domain.Operations;

namespace BG.Application.Intake;

internal sealed record IntakeScenarioDefinition(
    IntakeScenarioKind Kind,
    string Key,
    string TitleResourceKey,
    string SummaryResourceKey,
    string SaveOutcomeResourceKey,
    string HandoffResourceKey,
    GuaranteeDocumentType DocumentType,
    GuaranteeCorrespondenceKind? CorrespondenceKind,
    bool RequiresExistingGuarantee,
    GuaranteeRequestType? ExpectedRequestType,
    OperationsReviewItemCategory ReviewCategory,
    IReadOnlyList<string> RequiredReviewFieldKeys,
    IReadOnlyList<IntakeFieldReviewDto> SampleFields)
{
    public bool SupportsRequestMatching => ExpectedRequestType.HasValue;

    public bool RequiresConfirmedExpiryDate => Kind == IntakeScenarioKind.ExtensionConfirmation;

    public bool RequiresConfirmedAmount => Kind == IntakeScenarioKind.ReductionConfirmation;

    public bool RequiresStatusStatement => Kind == IntakeScenarioKind.StatusVerification;

    public bool RequiresAttachmentNote => Kind == IntakeScenarioKind.SupportingAttachment;

    public string? BuildDocumentNote(IntakeSubmissionCommand command)
    {
        return Kind switch
        {
            IntakeScenarioKind.ExtensionConfirmation =>
                $"Verified incoming extension confirmation. Proposed expiry date: {Normalize(command.NewExpiryDate)}.",
            IntakeScenarioKind.ReductionConfirmation =>
                $"Verified incoming reduction confirmation. Confirmed amount: {Normalize(command.Amount)}.",
            IntakeScenarioKind.ReleaseConfirmation => "Verified incoming bank release confirmation.",
            IntakeScenarioKind.StatusVerification => Normalize(command.StatusStatement),
            IntakeScenarioKind.SupportingAttachment => Normalize(command.AttachmentNote),
            _ => null
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
