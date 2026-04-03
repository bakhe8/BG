using System.Globalization;
using BG.Application.Common;
using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Application.ReferenceData;
using BG.Domain.Guarantees;
using BG.Web.Localization;
using BG.Web.Pages.Intake;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BG.UnitTests.Web;

public sealed class IntakeWorkspacePageTests
{
    [Fact]
    public async Task OnGetAsync_uses_requested_scenario_when_available()
    {
        var actor = new IntakeActorSummaryDto(Guid.NewGuid(), "intake.specialist", "Intake Specialist");
        var model = CreateModel(actor);
        model.Actor = actor.Id;
        model.Scenario = IntakeScenarioKeys.ReleaseConfirmation;

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(IntakeScenarioKeys.ReleaseConfirmation, model.SelectedScenario.Key);
        Assert.Equal(actor.Id, model.Input.IntakeActorUserId);
    }

    [Fact]
    public async Task OnGetAsync_falls_back_to_default_scenario_for_unknown_keys()
    {
        var actor = new IntakeActorSummaryDto(Guid.NewGuid(), "intake.specialist", "Intake Specialist");
        var model = CreateModel(actor);
        model.Actor = actor.Id;
        model.Scenario = "unknown";

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(IntakeScenarioKeys.NewGuarantee, model.SelectedScenario.Key);
    }

    [Fact]
    public async Task OnPostExtractAsync_populates_staged_draft_fields()
    {
        var actor = new IntakeActorSummaryDto(Guid.NewGuid(), "intake.specialist", "Intake Specialist");
        var model = CreateModel(actor);
        model.Input.IntakeActorUserId = actor.Id;
        model.Input.ScenarioKey = IntakeScenarioKeys.NewGuarantee;
        model.Input.UploadedDocument = CreateFormFile("BG-2026-0666.pdf");
        model.ModelState.SetModelValue("Input.GuaranteeNumber", string.Empty, string.Empty);
        model.ModelState.SetModelValue("Input.BankName", string.Empty, string.Empty);

        var result = await model.OnPostExtractAsync(CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>(result);
        Assert.True(model.HasDraft);
        Assert.Equal("token-1", model.Input.StagedDocumentToken);
        Assert.Equal(GuaranteeDocumentFormKeys.GuaranteeInstrumentSnb, model.Input.DocumentFormKey);
        Assert.Equal("BG-2026-0666", model.Input.GuaranteeNumber);
        Assert.DoesNotContain("Input.GuaranteeNumber", model.ModelState.Keys);
        Assert.DoesNotContain("Input.BankName", model.ModelState.Keys);
        Assert.Contains(
            model.ReviewFields,
            field => field.FieldKey == IntakeFieldKeys.GuaranteeNumber
                     && field.Value == "BG-2026-0666"
                     && field.IsExpectedByDocumentForm);
        Assert.Contains(
            model.ReviewFields,
            field => field.FieldKey == IntakeFieldKeys.BankName
                     && field.Value == "Saudi National Bank"
                     && field.IsExpectedByDocumentForm);
    }

    [Fact]
    public async Task OnPostSaveAsync_redirects_after_successful_submission()
    {
        var actor = new IntakeActorSummaryDto(Guid.NewGuid(), "intake.specialist", "Intake Specialist");
        var model = CreateModel(actor);
        model.Input.IntakeActorUserId = actor.Id;
        model.Input.ScenarioKey = IntakeScenarioKeys.SupportingAttachment;
        model.Input.StagedDocumentToken = "token-1";
        model.Input.ExtractionRouteResourceKey = "IntakeExtractionRoute_PdfTextFirst";
        model.Input.PageCount = 1;
        model.Input.CaptureChannel = GuaranteeDocumentCaptureChannel.ManualUpload;
        model.Input.GuaranteeNumber = "BG-2026-0001";
        model.Input.AttachmentNote = "Verified";

        var result = await model.OnPostSaveAsync(CancellationToken.None);

        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal("/Intake/Workspace", redirect.PageName);
        Assert.Equal(actor.Id, redirect.RouteValues!["actor"]);
        Assert.Equal(IntakeScenarioKeys.SupportingAttachment, redirect.RouteValues["scenario"]);
        Assert.Contains("BG-2026-0001", model.StatusMessage);
    }

    [Fact]
    public async Task OnGetPreviewAsync_returns_staged_document_stream_for_known_token()
    {
        var actor = new IntakeActorSummaryDto(Guid.NewGuid(), "intake.specialist", "Intake Specialist");
        var model = CreateModel(actor);

        var result = await model.OnGetPreviewAsync("token-1", CancellationToken.None);

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
    }

    private static WorkspaceModel CreateModel(IntakeActorSummaryDto actor)
    {
        return new WorkspaceModel(
            new StubIntakeWorkspaceService(actor),
            new StubIntakeSubmissionService(),
            new StubIntakeDocumentStore(),
            new StubStringLocalizer());
    }

    private static IFormFile CreateFormFile(string fileName)
    {
        var stream = new MemoryStream("scan"u8.ToArray());
        return new FormFile(stream, 0, stream.Length, "Input.UploadedDocument", fileName);
    }

    private sealed class StubIntakeWorkspaceService : IIntakeWorkspaceService
    {
        private readonly IntakeActorSummaryDto _actor;

        public StubIntakeWorkspaceService(IntakeActorSummaryDto actor)
        {
            _actor = actor;
        }

        public Task<IntakeWorkspaceSnapshotDto> GetWorkspaceAsync(
            Guid? intakeActorId = null,
            string? scenarioKey = null,
            CancellationToken cancellationToken = default)
        {
            var scenarios = CreateScenarios();
            var selectedScenario = scenarios.FirstOrDefault(scenario => scenario.Key == scenarioKey) ?? scenarios[0];

            return Task.FromResult(
                new IntakeWorkspaceSnapshotDto(
                    _actor,
                    [_actor],
                    "IntakeWorkspace_PrimaryRole",
                    "IntakeWorkspace_SaveMode",
                    "IntakeWorkspace_ReviewGate",
                    ["IntakeAction_Scan", "IntakeAction_Classify", "IntakeAction_Verify", "IntakeAction_Save"],
                    ["IntakeExcluded_RequestActions", "IntakeExcluded_Printing", "IntakeExcluded_Approvals", "IntakeExcluded_Dispatch"],
                    ["IntakePipeline_ClassifyPage"],
                    ["IntakeQuality_TextFirst"],
                    ["IntakeFuture_ScanStation"],
                    scenarios,
                    selectedScenario,
                    true,
                    "IntakeWorkspace_ActorScopedNotice"));
        }

        private static IReadOnlyList<IntakeScenarioSnapshotDto> CreateScenarios()
        {
            return
            [
                new(
                    IntakeScenarioKeys.NewGuarantee,
                    "IntakeScenario_NewGuarantee_Title",
                    "IntakeScenario_NewGuarantee_Summary",
                    "IntakeScenario_NewGuarantee_SaveOutcome",
                    "IntakeScenario_NewGuarantee_Handoff",
                    [IntakeFieldKeys.GuaranteeNumber],
                    [new IntakeFieldReviewDto(IntakeFieldKeys.GuaranteeNumber, IntakeFieldKeys.GuaranteeNumber, "BG-2026-0001", 99, true)],
                    RequiresExistingGuarantee: false,
                    RequiresConfirmedExpiryDate: false,
                    RequiresConfirmedAmount: false,
                    RequiresStatusStatement: false,
                    RequiresAttachmentNote: false,
                    DefaultDocumentFormKey: GuaranteeDocumentFormKeys.GuaranteeInstrumentGeneric,
                    SupportedDocumentForms:
                    [
                        new IntakeDocumentFormOptionDto(
                            GuaranteeDocumentFormKeys.GuaranteeInstrumentGeneric,
                            "BankProfile_Generic",
                            "DocumentForm_Instrument_Generic_Title",
                            "DocumentForm_Instrument_Generic_Summary",
                            [IntakeFieldKeys.GuaranteeNumber],
                            ["DocumentFormCue_BankIdentity"]),
                        new IntakeDocumentFormOptionDto(
                            GuaranteeDocumentFormKeys.GuaranteeInstrumentSnb,
                            "BankProfile_SNB",
                            "DocumentForm_Instrument_SNB_Title",
                            "DocumentForm_Instrument_SNB_Summary",
                            [IntakeFieldKeys.GuaranteeNumber, IntakeFieldKeys.BankName],
                            ["DocumentFormCue_BankIdentity"])
                    ]),
                new(
                    IntakeScenarioKeys.ReleaseConfirmation,
                    "IntakeScenario_Release_Title",
                    "IntakeScenario_Release_Summary",
                    "IntakeScenario_Release_SaveOutcome",
                    "IntakeScenario_Release_Handoff",
                    [IntakeFieldKeys.BankReference],
                    [new IntakeFieldReviewDto(IntakeFieldKeys.BankReference, IntakeFieldKeys.BankReference, "REL-1001", 98, true)],
                    RequiresExistingGuarantee: true,
                    RequiresConfirmedExpiryDate: false,
                    RequiresConfirmedAmount: false,
                    RequiresStatusStatement: false,
                    RequiresAttachmentNote: false,
                    DefaultDocumentFormKey: GuaranteeDocumentFormKeys.BankLetterGeneric,
                    SupportedDocumentForms:
                    [
                        new IntakeDocumentFormOptionDto(
                            GuaranteeDocumentFormKeys.BankLetterGeneric,
                            "BankProfile_Generic",
                            "DocumentForm_BankLetter_Generic_Title",
                            "DocumentForm_BankLetter_Generic_Summary",
                            [IntakeFieldKeys.GuaranteeNumber, IntakeFieldKeys.BankReference],
                            ["DocumentFormCue_BankReference"])
                    ]),
                new(
                    IntakeScenarioKeys.SupportingAttachment,
                    "IntakeScenario_Attachment_Title",
                    "IntakeScenario_Attachment_Summary",
                    "IntakeScenario_Attachment_SaveOutcome",
                    "IntakeScenario_Attachment_Handoff",
                    [IntakeFieldKeys.AttachmentNote],
                    [new IntakeFieldReviewDto(IntakeFieldKeys.AttachmentNote, IntakeFieldKeys.AttachmentNote, "Verified", 97, true)],
                    RequiresExistingGuarantee: true,
                    RequiresConfirmedExpiryDate: false,
                    RequiresConfirmedAmount: false,
                    RequiresStatusStatement: false,
                    RequiresAttachmentNote: true,
                    DefaultDocumentFormKey: GuaranteeDocumentFormKeys.SupportingAttachmentGeneric,
                    SupportedDocumentForms:
                    [
                        new IntakeDocumentFormOptionDto(
                            GuaranteeDocumentFormKeys.SupportingAttachmentGeneric,
                            "BankProfile_Generic",
                            "DocumentForm_Attachment_Generic_Title",
                            "DocumentForm_Attachment_Generic_Summary",
                            [IntakeFieldKeys.GuaranteeNumber, IntakeFieldKeys.AttachmentNote],
                            ["DocumentFormCue_AttachmentPurpose"])
                    ])
            ];
        }
    }

    private sealed class StubIntakeSubmissionService : IIntakeSubmissionService
    {
        public Task<OperationResult<IntakeExtractionDraftDto>> BeginExtractionAsync(BeginIntakeExtractionCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                OperationResult<IntakeExtractionDraftDto>.Success(
                    new IntakeExtractionDraftDto(
                        command.ScenarioKey,
                        "token-1",
                        command.FileName,
                        1,
                        "IntakeExtractionRoute_PdfTextFirst",
                        [
                            new(IntakeFieldKeys.GuaranteeNumber, IntakeFieldKeys.GuaranteeNumber, "BG-2026-0666", 99, true),
                            new(IntakeFieldKeys.BankName, IntakeFieldKeys.BankName, "Saudi National Bank", 97, true)
                        ],
                        GuaranteeDocumentFormKeys.GuaranteeInstrumentSnb)));
        }

        public Task<OperationResult<IntakeSubmissionReceiptDto>> FinalizeAsync(IntakeSubmissionCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                OperationResult<IntakeSubmissionReceiptDto>.Success(
                    new IntakeSubmissionReceiptDto(
                        Guid.NewGuid(),
                        command.GuaranteeNumber ?? "BG-2026-0001",
                        Guid.NewGuid(),
                        command.ScenarioKey,
                        "IntakeScenario_Attachment_Handoff")));
        }
    }

    private sealed class StubIntakeDocumentStore : IIntakeDocumentStore
    {
        public Task<StagedIntakeDocumentDto> StageAsync(string originalFileName, Stream content, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<StagedIntakeDocumentDto?> GetStagedAsync(string stagedDocumentToken, CancellationToken cancellationToken = default)
        {
            var tempPath = Path.GetTempFileName();
            File.WriteAllBytes(tempPath, "%PDF-1.4\n%BG test\n"u8.ToArray());
            return Task.FromResult<StagedIntakeDocumentDto?>(
                new StagedIntakeDocumentDto(stagedDocumentToken, "preview.pdf", 16, tempPath));
        }

        public Task<PromotedIntakeDocumentDto> PromoteAsync(string stagedDocumentToken, string guaranteeNumber, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Stream GetDocumentContent(string storagePath)
        {
            if (File.Exists(storagePath))
            {
                return File.OpenRead(storagePath);
            }

            return new MemoryStream("%PDF-1.4\n%BG test\n"u8.ToArray());
        }
    }

    private sealed class StubStringLocalizer : IStringLocalizer<SharedResource>
    {
        public LocalizedString this[string name] => new(name, name, false);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.InvariantCulture, name == "IntakeWorkspace_SaveSuccess" ? "Intake saved successfully for guarantee {0}." : name, arguments), false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return Array.Empty<LocalizedString>();
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            return this;
        }
    }
}
