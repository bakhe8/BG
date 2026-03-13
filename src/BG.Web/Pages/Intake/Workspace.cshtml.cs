using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Application.ReferenceData;
using BG.Domain.Guarantees;
using BG.Web.Localization;
using BG.Web.Security;
using BG.Web.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace BG.Web.Pages.Intake;

[Authorize(Policy = PermissionPolicyNames.IntakeWorkspace)]
public sealed class WorkspaceModel : PageModel
{
    private readonly IIntakeWorkspaceService _intakeWorkspaceService;
    private readonly IIntakeSubmissionService _intakeSubmissionService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public WorkspaceModel(
        IIntakeWorkspaceService intakeWorkspaceService,
        IIntakeSubmissionService intakeSubmissionService,
        IStringLocalizer<SharedResource> localizer)
    {
        _intakeWorkspaceService = intakeWorkspaceService;
        _intakeSubmissionService = intakeSubmissionService;
        _localizer = localizer;
    }

    [FromQuery(Name = "actor")]
    public Guid? Actor { get; set; }

    [FromQuery(Name = "scenario")]
    public string? Scenario { get; set; }

    [BindProperty]
    public IntakeSubmissionInput Input { get; set; } = new();

    public IntakeWorkspaceSnapshotDto Workspace { get; private set; } = default!;

    public IntakeScenarioSnapshotDto SelectedScenario { get; private set; } = default!;

    public IReadOnlyList<IntakeFieldReviewDto> ReviewFields { get; private set; } = Array.Empty<IntakeFieldReviewDto>();

    public IReadOnlyList<IntakeFieldReviewDto> CriticalReviewFields
        => ReviewFields.Where(IsCriticalReviewField).ToArray();

    public IReadOnlyList<IntakeFieldReviewDto> SupportingReviewFields
        => ReviewFields.Where(field => !IsCriticalReviewField(field)).ToArray();

    [TempData]
    public string? StatusMessage { get; set; }

    public bool IsActorContextLocked { get; private set; }

    public bool HasDraft => !string.IsNullOrWhiteSpace(Input.StagedDocumentToken);

    public bool CanFinalize => HasDraft && Workspace.HasEligibleActor;

    public bool IsNewGuaranteeScenario => !SelectedScenario.RequiresExistingGuarantee;

    public bool IsExtensionScenario => SelectedScenario.RequiresConfirmedExpiryDate;

    public bool IsReductionScenario => SelectedScenario.RequiresConfirmedAmount;

    public bool IsStatusScenario => SelectedScenario.RequiresStatusStatement;

    public bool IsAttachmentScenario => SelectedScenario.RequiresAttachmentNote;

    public IntakeDocumentFormOptionDto SelectedDocumentForm
        => ResolveSelectedDocumentForm();

    public int ReviewAttentionCount => ReviewFields.Count(field => field.RequiresExplicitReview || field.ConfidencePercent < 95);

    public int HighConfidenceCount => ReviewFields.Count(field => field.ConfidencePercent >= 95);

    public int ExpectedFormFieldCount => ReviewFields.Count(field => field.IsExpectedByDocumentForm);

    public int SupportingReviewFieldCount => SupportingReviewFields.Count;

    public string CaptureChannelHintResourceKey => GuaranteeResourceCatalog.GetCaptureChannelHintResourceKey(Input.CaptureChannel);

    public IReadOnlyList<CaptureChannelOptionViewModel> CaptureChannelOptions { get; } =
        GuaranteeResourceCatalog.GetSupportedCaptureChannels()
            .Select(channel => new CaptureChannelOptionViewModel(channel, GuaranteeResourceCatalog.GetCaptureChannelResourceKey(channel)))
            .ToArray();

    public IReadOnlyList<GuaranteeCategoryOptionViewModel> GuaranteeCategoryOptions { get; } =
        GuaranteeResourceCatalog.GetSupportedGuaranteeCategories()
            .Select(category => new GuaranteeCategoryOptionViewModel(category, GuaranteeResourceCatalog.GetGuaranteeCategoryResourceKey(category)))
            .ToArray();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadWorkspaceAsync(Actor, Scenario, cancellationToken);
        ReviewFields = BuildReviewFields();
    }

    public async Task<IActionResult> OnPostExtractAsync(CancellationToken cancellationToken)
    {
        await LoadWorkspaceAsync(
            Input.IntakeActorUserId == Guid.Empty ? Actor : Input.IntakeActorUserId,
            Input.ScenarioKey,
            cancellationToken);
        Input.IntakeActorUserId = Workspace.ActiveActor?.Id ?? Input.IntakeActorUserId;

        if (Input.UploadedDocument is null)
        {
            ModelState.AddModelError(string.Empty, _localizer[IntakeErrorCodes.FileRequired]);
            ReviewFields = BuildReviewFields();
            return Page();
        }

        await using var stream = Input.UploadedDocument.OpenReadStream();

        var result = await _intakeSubmissionService.BeginExtractionAsync(
            new BeginIntakeExtractionCommand(
                Input.ScenarioKey,
                Input.UploadedDocument.FileName,
                stream),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
            ReviewFields = BuildReviewFields();
            return Page();
        }

        ApplyDraft(result.Value!);
        ReviewFields = BuildReviewFields();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken)
    {
        await LoadWorkspaceAsync(
            Input.IntakeActorUserId == Guid.Empty ? Actor : Input.IntakeActorUserId,
            Input.ScenarioKey,
            cancellationToken);

        if (!Workspace.HasEligibleActor || Workspace.ActiveActor is null)
        {
            ModelState.AddModelError(string.Empty, _localizer["IntakeWorkspace_NoEligibleActor"]);
            ReviewFields = BuildReviewFields();
            return Page();
        }

        Input.IntakeActorUserId = Workspace.ActiveActor.Id;

        var result = await _intakeSubmissionService.FinalizeAsync(Input.ToCommand(), cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
            ReviewFields = BuildReviewFields();
            return Page();
        }

        StatusMessage = _localizer["IntakeWorkspace_SaveSuccess", result.Value!.GuaranteeNumber];
        return RedirectToSelf(Input.IntakeActorUserId, SelectedScenario.Key);
    }

    private async Task LoadWorkspaceAsync(Guid? actorId, string? scenarioKey, CancellationToken cancellationToken)
    {
        actorId = ResolveActor(actorId);
        Workspace = await _intakeWorkspaceService.GetWorkspaceAsync(actorId, scenarioKey, cancellationToken);
        SelectedScenario = Workspace.SelectedScenario;
        Input.ScenarioKey = SelectedScenario.Key;
        NormalizeDocumentFormSelection();
        if (Workspace.ActiveActor is not null)
        {
            Input.IntakeActorUserId = Workspace.ActiveActor.Id;
        }
    }

    private Guid? ResolveActor(Guid? actorId)
    {
        var lockedActorId = WorkspaceActorContext.TryGetLockedActorUserId(HttpContext);
        IsActorContextLocked = lockedActorId.HasValue;
        return lockedActorId ?? actorId;
    }

    private RedirectToPageResult RedirectToSelf(Guid actorId, string scenarioKey)
    {
        return IsActorContextLocked
            ? RedirectToPage("/Intake/Workspace", new { scenario = scenarioKey })
            : RedirectToPage("/Intake/Workspace", new { actor = actorId, scenario = scenarioKey });
    }

    private void ApplyDraft(IntakeExtractionDraftDto draft)
    {
        Input.ScenarioKey = draft.ScenarioKey;
        Input.StagedDocumentToken = draft.StagedDocumentToken;
        Input.DocumentFormKey = draft.DocumentFormKey;
        Input.OriginalFileName = draft.OriginalFileName;
        Input.PageCount = draft.PageCount;
        Input.ExtractionRouteResourceKey = draft.ExtractionRouteResourceKey;

        foreach (var field in draft.Fields)
        {
            switch (field.FieldKey)
            {
                case IntakeFieldKeys.GuaranteeNumber:
                    Input.GuaranteeNumber = field.Value;
                    break;
                case IntakeFieldKeys.BankName:
                    Input.BankName = field.Value;
                    break;
                case IntakeFieldKeys.Beneficiary:
                    Input.BeneficiaryName = field.Value;
                    break;
                case IntakeFieldKeys.Principal:
                    Input.PrincipalName = field.Value;
                    break;
                case IntakeFieldKeys.Amount:
                    Input.Amount = field.Value;
                    break;
                case IntakeFieldKeys.GuaranteeCategory:
                    if (Enum.TryParse<GuaranteeCategory>(field.Value, ignoreCase: true, out var category))
                    {
                        Input.GuaranteeCategory = category;
                    }
                    break;
                case IntakeFieldKeys.CurrencyCode:
                    Input.CurrencyCode = field.Value;
                    break;
                case IntakeFieldKeys.IssueDate:
                    Input.IssueDate = field.Value;
                    break;
                case IntakeFieldKeys.ExpiryDate:
                    Input.ExpiryDate = field.Value;
                    break;
                case IntakeFieldKeys.OfficialLetterDate:
                    Input.OfficialLetterDate = field.Value;
                    break;
                case IntakeFieldKeys.NewExpiryDate:
                    Input.NewExpiryDate = field.Value;
                    break;
                case IntakeFieldKeys.BankReference:
                    Input.BankReference = field.Value;
                    break;
                case IntakeFieldKeys.StatusStatement:
                    Input.StatusStatement = field.Value;
                    break;
                case IntakeFieldKeys.AttachmentNote:
                    Input.AttachmentNote = field.Value;
                    break;
            }
        }
    }

    private IReadOnlyList<IntakeFieldReviewDto> BuildReviewFields()
    {
        var expectedFieldKeys = SelectedDocumentForm.ExpectedFieldKeys.ToHashSet(StringComparer.Ordinal);

        return SelectedScenario.SampleFields
            .Select(
                field => field with
                {
                    Value = ResolveCurrentValue(field.FieldKey) ?? field.Value,
                    IsExpectedByDocumentForm = expectedFieldKeys.Contains(field.FieldKey)
                })
            .ToArray();
    }

    private void NormalizeDocumentFormSelection()
    {
        if (SelectedScenario.SupportedDocumentForms.Count == 0)
        {
            Input.DocumentFormKey = string.Empty;
            return;
        }

        if (string.IsNullOrWhiteSpace(Input.DocumentFormKey) ||
            !SelectedScenario.SupportedDocumentForms.Any(
                form => string.Equals(form.Key, Input.DocumentFormKey, StringComparison.OrdinalIgnoreCase)))
        {
            Input.DocumentFormKey = SelectedScenario.DefaultDocumentFormKey;
        }
    }

    private IntakeDocumentFormOptionDto ResolveSelectedDocumentForm()
    {
        if (SelectedScenario.SupportedDocumentForms.Count == 0)
        {
            return new IntakeDocumentFormOptionDto(
                string.Empty,
                "BankProfile_Generic",
                "DocumentForm_Instrument_Generic_Title",
                "DocumentForm_Instrument_Generic_Summary",
                [],
                []);
        }

        return SelectedScenario.SupportedDocumentForms.FirstOrDefault(
                   form => string.Equals(form.Key, Input.DocumentFormKey, StringComparison.OrdinalIgnoreCase))
               ?? SelectedScenario.SupportedDocumentForms[0];
    }

    private string? ResolveCurrentValue(string fieldKey)
    {
        return fieldKey switch
        {
            IntakeFieldKeys.GuaranteeNumber => Input.GuaranteeNumber,
            IntakeFieldKeys.BankName => Input.BankName,
            IntakeFieldKeys.Beneficiary => Input.BeneficiaryName,
            IntakeFieldKeys.Principal => Input.PrincipalName,
            IntakeFieldKeys.GuaranteeCategory => Input.GuaranteeCategory?.ToString(),
            IntakeFieldKeys.Amount => Input.Amount,
            IntakeFieldKeys.CurrencyCode => Input.CurrencyCode,
            IntakeFieldKeys.IssueDate => Input.IssueDate,
            IntakeFieldKeys.ExpiryDate => Input.ExpiryDate,
            IntakeFieldKeys.OfficialLetterDate => Input.OfficialLetterDate,
            IntakeFieldKeys.NewExpiryDate => Input.NewExpiryDate,
            IntakeFieldKeys.BankReference => Input.BankReference,
            IntakeFieldKeys.StatusStatement => Input.StatusStatement,
            IntakeFieldKeys.AttachmentNote => Input.AttachmentNote,
            _ => null
        };
    }

    private static bool IsCriticalReviewField(IntakeFieldReviewDto field)
    {
        return field.RequiresExplicitReview
               || field.ConfidencePercent < 95
               || field.IsExpectedByDocumentForm;
    }

    public sealed class IntakeSubmissionInput
    {
        public Guid IntakeActorUserId { get; set; }

        public string ScenarioKey { get; set; } = IntakeScenarioKeys.NewGuarantee;

        public IFormFile? UploadedDocument { get; set; }

        public string? StagedDocumentToken { get; set; }

        public string DocumentFormKey { get; set; } = GuaranteeDocumentFormKeys.GuaranteeInstrumentGeneric;

        public string? OriginalFileName { get; set; }

        public string? ExtractionRouteResourceKey { get; set; }

        public int PageCount { get; set; }

        public GuaranteeDocumentCaptureChannel CaptureChannel { get; set; } = GuaranteeDocumentCaptureChannel.ManualUpload;

        public string? SourceSystemName { get; set; }

        public string? SourceReference { get; set; }

        public string? GuaranteeNumber { get; set; }

        public string? BankName { get; set; }

        public string? BeneficiaryName { get; set; }

        public string? PrincipalName { get; set; }

        public GuaranteeCategory? GuaranteeCategory { get; set; }

        public string? Amount { get; set; }

        public string? CurrencyCode { get; set; }

        public string? IssueDate { get; set; }

        public string? ExpiryDate { get; set; }

        public string? OfficialLetterDate { get; set; }

        public string? NewExpiryDate { get; set; }

        public string? BankReference { get; set; }

        public string? StatusStatement { get; set; }

        public string? AttachmentNote { get; set; }

        public IntakeSubmissionCommand ToCommand()
        {
            return new IntakeSubmissionCommand(
                IntakeActorUserId,
                ScenarioKey,
                StagedDocumentToken ?? string.Empty,
                DocumentFormKey,
                ExtractionRouteResourceKey ?? string.Empty,
                PageCount,
                CaptureChannel,
                SourceSystemName,
                SourceReference,
                GuaranteeNumber,
                BankName,
                BeneficiaryName,
                PrincipalName,
                GuaranteeCategory,
                Amount,
                CurrencyCode,
                IssueDate,
                ExpiryDate,
                OfficialLetterDate,
                NewExpiryDate,
                BankReference,
                StatusStatement,
                AttachmentNote);
        }
    }

    public sealed record CaptureChannelOptionViewModel(GuaranteeDocumentCaptureChannel Value, string ResourceKey);

    public sealed record GuaranteeCategoryOptionViewModel(GuaranteeCategory Value, string ResourceKey);
}
