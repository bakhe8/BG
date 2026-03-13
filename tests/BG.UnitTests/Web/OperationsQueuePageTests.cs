using BG.Application.Common;
using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Models.Documents;
using BG.Application.Operations;
using BG.Web.Localization;
using BG.Web.Pages.Operations;
using BG.Web.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace BG.UnitTests.Web;

public sealed class OperationsQueuePageTests
{
    [Fact]
    public async Task OnGetAsync_loads_queue_snapshot()
    {
        var actor = new OperationsActorSummaryDto(Guid.NewGuid(), "operations.reviewer", "Operations Reviewer");
        var model = new QueueModel(new StubOperationsReviewQueueService(actor), new PassThroughLocalizer())
        {
            Actor = actor.Id
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.True(model.Snapshot.HasEligibleActor);
        Assert.Equal(actor.Id, model.Snapshot.ActiveActor!.Id);
        Assert.Single(model.Snapshot.Items);
        Assert.NotEmpty(model.Snapshot.WorkflowTemplates);
        Assert.Equal("BG-2026-2001", model.Snapshot.Items[0].GuaranteeNumber);
    }

    [Fact]
    public async Task OnPostApplyAsync_redirects_after_successful_application()
    {
        var actor = new OperationsActorSummaryDto(Guid.NewGuid(), "operations.reviewer", "Operations Reviewer");
        var service = new StubOperationsReviewQueueService(actor);
        var model = new QueueModel(service, new PassThroughLocalizer());
        var reviewItemId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var result = await model.OnPostApplyAsync(
            actor.Id,
            reviewItemId,
            requestId,
            "2027-12-31",
            null,
            null,
            "Applied",
            null,
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Operations/Queue", redirect.PageName);
        Assert.Equal(actor.Id, redirect.RouteValues!["actor"]);
        Assert.Equal(actor.Id, service.LastCommand!.OperationsActorUserId);
        Assert.Equal(reviewItemId, service.LastCommand.ReviewItemId);
        Assert.Equal(requestId, service.LastCommand.RequestId);
        Assert.Equal("OperationsQueue_ApplySuccess", model.StatusMessage);
    }

    [Fact]
    public async Task OnPostApplyAsync_uses_locked_actor_from_authenticated_session()
    {
        var lockedActor = new OperationsActorSummaryDto(Guid.NewGuid(), "operations.reviewer", "Operations Reviewer");
        var service = new StubOperationsReviewQueueService(lockedActor);
        var model = new QueueModel(service, new PassThroughLocalizer());
        AttachAuthenticatedUser(model, lockedActor.Id);

        var result = await model.OnPostApplyAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "2027-12-31",
            null,
            null,
            "Applied",
            null,
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Operations/Queue", redirect.PageName);
        Assert.True(redirect.RouteValues is null || !redirect.RouteValues.ContainsKey("actor"));
        Assert.True(model.IsActorContextLocked);
        Assert.Equal(lockedActor.Id, service.LastCommand!.OperationsActorUserId);
    }

    [Fact]
    public async Task OnPostApplyAsync_returns_page_with_localized_error_when_application_is_blocked()
    {
        var actor = new OperationsActorSummaryDto(Guid.NewGuid(), "operations.reviewer", "Operations Reviewer");
        var service = new StubOperationsReviewQueueService(actor)
        {
            NextResult = OperationResult<ApplyBankResponseReceiptDto>.Failure(
                OperationsReviewErrorCodes.ResponseBankProfileMismatch)
        };
        var model = new QueueModel(service, new PassThroughLocalizer());

        var result = await model.OnPostApplyAsync(
            actor.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "2027-12-31",
            null,
            null,
            "Applied",
            null,
            CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Contains(
            model.ModelState[string.Empty]!.Errors,
            error => error.ErrorMessage == OperationsReviewErrorCodes.ResponseBankProfileMismatch);
    }

    private static void AttachAuthenticatedUser(PageModel model, Guid userId)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            ],
            WorkspaceShellDefaults.AuthenticationScheme));

        model.PageContext = new PageContext
        {
            HttpContext = httpContext
        };
    }

    private sealed class StubOperationsReviewQueueService : IOperationsReviewQueueService
    {
        private readonly OperationsActorSummaryDto _actor;

        public StubOperationsReviewQueueService(OperationsActorSummaryDto actor)
        {
            _actor = actor;
        }

        public ApplyBankResponseCommand? LastCommand { get; private set; }
        public OperationResult<ApplyBankResponseReceiptDto>? NextResult { get; set; }

        public Task<OperationsReviewQueueSnapshotDto> GetSnapshotAsync(
            Guid? operationsActorId,
            int pageNumber = 1,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new OperationsReviewQueueSnapshotDto(
                    _actor,
                    [_actor],
                    [
                        new OperationsReviewItemDto(
                            Guid.NewGuid(),
                            "BG-2026-2001",
                            IntakeScenarioKeys.NewGuarantee,
                            "IntakeScenario_NewGuarantee_Title",
                            "OperationsReviewCategory_GuaranteeRegistration",
                            "OperationsReviewStatus_Pending",
                            "OperationsReviewLane_RegistrationReview",
                            "instrument.pdf",
                            null,
                            DateTimeOffset.UtcNow,
                            DateTimeOffset.UtcNow,
                            "Intake Specialist",
                            "IntakeCaptureChannel_ScanStation",
                            "Scan Station",
                            "batch-1",
                            new GuaranteeDocumentFormSnapshotDto(
                                "guarantee-instrument-snb",
                                "BankProfile_SNB",
                                "DocumentForm_Instrument_SNB_Title",
                                "DocumentForm_Instrument_SNB_Summary"),
                            false,
                            [],
                            null,
                            null,
                            null,
                            false,
                            false)
                    ],
                    new PageInfoDto(pageNumber, 10, 1),
                    [
                        new RequestWorkflowTemplateDto(
                            "Extend",
                            BG.Domain.Guarantees.GuaranteeRequestType.Extend,
                            null,
                            null,
                            "WorkflowTemplate_Extend_Title",
                            "WorkflowTemplate_Extend_Summary",
                            [
                                new RequestWorkflowStageTemplateDto(
                                    1,
                                    "WorkflowStage_GuaranteesSupervisor_Title",
                                    "WorkflowStage_GuaranteesSupervisor_Summary",
                                    true,
                                    "WorkflowSignatureMode_ButtonStampedPdf",
                                    "WorkflowSignatureEffect_FinalLetterPdf")
                            ])
                    ],
                    1,
                    1,
                    0,
                    true,
                    "OperationsQueue_ActorScopedNotice"));
        }

        public Task<BG.Application.Common.OperationResult<ApplyBankResponseReceiptDto>> ApplyBankResponseAsync(
            ApplyBankResponseCommand command,
            CancellationToken cancellationToken = default)
        {
            LastCommand = command;
            return Task.FromResult(
                NextResult ??
                BG.Application.Common.OperationResult<ApplyBankResponseReceiptDto>.Success(
                    new ApplyBankResponseReceiptDto(command.ReviewItemId, command.RequestId, "BG-2026-2001")));
        }
    }

    private sealed class PassThroughLocalizer : IStringLocalizer<SharedResource>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return Array.Empty<LocalizedString>();
        }

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture)
        {
            return this;
        }
    }
}
