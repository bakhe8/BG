using BG.Application.Common;
using BG.Application.Contracts.Services;
using BG.Application.Models.Documents;
using BG.Application.Models.Dispatch;
using BG.Domain.Guarantees;
using BG.Web.Localization;
using BG.Web.Pages.Dispatch;
using BG.Web.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace BG.UnitTests.Web;

public sealed class DispatchWorkspacePageTests
{
    [Fact]
    public async Task OnGetAsync_loads_dispatch_queue_for_active_actor()
    {
        var actorId = Guid.NewGuid();
        var model = new WorkspaceModel(new StubDispatchWorkspaceService(actorId), new PassThroughLocalizer());

        await model.OnGetAsync(CancellationToken.None);

        Assert.True(model.Snapshot.HasEligibleActor);
        Assert.Equal(actorId, model.Snapshot.ActiveActor!.Id);
        Assert.Single(model.Snapshot.Items);
    }

    [Fact]
    public async Task OnPostPrintAsync_redirects_back_to_workspace_for_actor()
    {
        var actorId = Guid.NewGuid();
        var service = new StubDispatchWorkspaceService(actorId);
        var model = new WorkspaceModel(service, new PassThroughLocalizer());

        var result = await model.OnPostPrintAsync(
            actorId,
            Guid.NewGuid(),
            "LTR-8001",
            "2026-03-12",
            GuaranteeOutgoingLetterPrintMode.WorkstationPrinter,
            null,
            null,
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Dispatch/Workspace", redirect.PageName);
        Assert.Equal(actorId, redirect.RouteValues!["actor"]);
        Assert.Equal(actorId, service.LastPrintCommand!.DispatcherUserId);
        Assert.Equal("DispatchWorkspace_PrintSuccess", model.StatusMessage);
    }

    [Fact]
    public async Task OnPostDispatchAsync_uses_locked_actor_from_authenticated_session()
    {
        var lockedActorId = Guid.NewGuid();
        var service = new StubDispatchWorkspaceService(lockedActorId);
        var model = new WorkspaceModel(service, new PassThroughLocalizer());
        AttachAuthenticatedUser(model, lockedActorId);

        var result = await model.OnPostDispatchAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "LTR-8001",
            "2026-03-12",
            GuaranteeDispatchChannel.Courier,
            "PKG-1",
            "Sent",
            null,
            null,
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Dispatch/Workspace", redirect.PageName);
        Assert.True(redirect.RouteValues is null || !redirect.RouteValues.ContainsKey("actor"));
        Assert.True(model.IsActorContextLocked);
        Assert.Equal(lockedActorId, service.LastDispatchCommand!.DispatcherUserId);
    }

    [Fact]
    public async Task OnPostConfirmDeliveryAsync_redirects_back_to_workspace_for_actor()
    {
        var actorId = Guid.NewGuid();
        var service = new StubDispatchWorkspaceService(actorId);
        var model = new WorkspaceModel(service, new PassThroughLocalizer());

        var result = await model.OnPostConfirmDeliveryAsync(
            actorId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "REC-1",
            "Delivered",
            null,
            null,
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Dispatch/Workspace", redirect.PageName);
        Assert.Equal(actorId, redirect.RouteValues!["actor"]);
        Assert.Equal(actorId, service.LastDeliveryCommand!.DispatcherUserId);
        Assert.Equal("DispatchWorkspace_DeliverySuccess", model.StatusMessage);
    }

    [Fact]
    public async Task OnPostReopenDispatchAsync_uses_locked_actor_from_authenticated_session()
    {
        var lockedActorId = Guid.NewGuid();
        var service = new StubDispatchWorkspaceService(lockedActorId);
        var model = new WorkspaceModel(service, new PassThroughLocalizer());
        AttachAuthenticatedUser(model, lockedActorId);

        var result = await model.OnPostReopenDispatchAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Recorded too early",
            null,
            null,
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Dispatch/Workspace", redirect.PageName);
        Assert.True(redirect.RouteValues is null || !redirect.RouteValues.ContainsKey("actor"));
        Assert.True(model.IsActorContextLocked);
        Assert.Equal(lockedActorId, service.LastReopenCommand!.DispatcherUserId);
        Assert.Equal("DispatchWorkspace_ReopenSuccess", model.StatusMessage);
    }

    [Fact]
    public async Task ResolveNoActionReasonResourceKey_returns_expected_reason_for_each_dispatch_lane()
    {
        var actorId = Guid.NewGuid();
        var model = new WorkspaceModel(
            new StubDispatchWorkspaceService(actorId, canPrint: false, canRecord: false, canEmail: false),
            new PassThroughLocalizer());

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal("DispatchWorkspace_NoAvailableReadyActionsReason", model.ResolveNoActionReasonResourceKey(isPendingDelivery: false));
        Assert.Equal("DispatchWorkspace_NoAvailablePendingActionsReason", model.ResolveNoActionReasonResourceKey(isPendingDelivery: true));
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

    private sealed class StubDispatchWorkspaceService : IDispatchWorkspaceService
    {
        private readonly Guid _actorId;

        public StubDispatchWorkspaceService(Guid actorId, bool canPrint = true, bool canRecord = true, bool canEmail = false)
        {
            _actorId = actorId;
            _canPrint = canPrint;
            _canRecord = canRecord;
            _canEmail = canEmail;
        }

        private readonly bool _canPrint;
        private readonly bool _canRecord;
        private readonly bool _canEmail;

        public PrintDispatchLetterCommand? LastPrintCommand { get; private set; }

        public RecordDispatchCommand? LastDispatchCommand { get; private set; }

        public ConfirmDispatchDeliveryCommand? LastDeliveryCommand { get; private set; }

        public ReopenDispatchCommand? LastReopenCommand { get; private set; }

        public Task<DispatchWorkspaceSnapshotDto> GetWorkspaceAsync(
            Guid? dispatcherActorId,
            int pageNumber = 1,
            CancellationToken cancellationToken = default)
        {
            var actor = new DispatchActorSummaryDto(_actorId, "dispatch.one", "Dispatch One", _canPrint, _canRecord, _canEmail);

            return Task.FromResult(
                new DispatchWorkspaceSnapshotDto(
                    actor,
                    [actor],
                    [
                        new DispatchQueueItemDto(
                            Guid.NewGuid(),
                            "BG-2026-7101",
                            "GuaranteeCategory_Contract",
                            "RequestType_Extend",
                            "RequestStatus_ApprovedForDispatch",
                            "Request Owner",
                            DateTimeOffset.UtcNow.AddHours(-1),
                            null,
                            null,
                            null,
                            new GuaranteeDocumentFormSnapshotDto(
                                "guarantee-instrument-snb",
                                "BankProfile_SNB",
                                "DocumentForm_Instrument_SNB_Title",
                                "DocumentForm_Instrument_SNB_Summary"),
                            0,
                            null,
                            null)
                    ],
                    [],
                    new PageInfoDto(pageNumber, 10, 1),
                    true,
                    "DispatchWorkspace_ActorScopedNotice"));
        }

        public Task<OperationResult<DispatchLetterPreviewDto>> GetLetterPreviewAsync(
            Guid dispatcherUserId,
            Guid requestId,
            string? referenceNumber,
            string? letterDate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                OperationResult<DispatchLetterPreviewDto>.Success(
                    new DispatchLetterPreviewDto(
                        requestId,
                        "BG-2026-7101",
                        "National Bank",
                        "KFSHRC",
                        "Prime Contractor",
                        "SAR",
                        150000m,
                        new DateOnly(2026, 1, 1),
                        new DateOnly(2026, 12, 31),
                        GuaranteeRequestType.Extend,
                        "Request Owner",
                        referenceNumber ?? "LTR-8001",
                        new DateOnly(2026, 3, 12),
                        null,
                        new DateOnly(2027, 6, 30),
                        "Ready for dispatch",
                        "Dispatch One",
                        DateTimeOffset.UtcNow,
                        true,
                        0)));
        }

        public Task<OperationResult<PrintDispatchLetterReceiptDto>> PrintDispatchLetterAsync(
            PrintDispatchLetterCommand command,
            CancellationToken cancellationToken = default)
        {
            LastPrintCommand = command;
            return Task.FromResult(
                OperationResult<PrintDispatchLetterReceiptDto>.Success(
                    new PrintDispatchLetterReceiptDto(command.RequestId, "BG-2026-7101", command.ReferenceNumber ?? "LTR-8001", 1)));
        }

        public Task<OperationResult<RecordDispatchReceiptDto>> RecordDispatchAsync(
            RecordDispatchCommand command,
            CancellationToken cancellationToken = default)
        {
            LastDispatchCommand = command;
            return Task.FromResult(
                OperationResult<RecordDispatchReceiptDto>.Success(
                    new RecordDispatchReceiptDto(command.RequestId, "BG-2026-7101", command.ReferenceNumber ?? "LTR-8001")));
        }

        public Task<OperationResult<ConfirmDispatchDeliveryReceiptDto>> ConfirmDeliveryAsync(
            ConfirmDispatchDeliveryCommand command,
            CancellationToken cancellationToken = default)
        {
            LastDeliveryCommand = command;
            return Task.FromResult(
                OperationResult<ConfirmDispatchDeliveryReceiptDto>.Success(
                    new ConfirmDispatchDeliveryReceiptDto(command.RequestId, "BG-2026-7101", "LTR-8001")));
        }

        public Task<OperationResult<ReopenDispatchReceiptDto>> ReopenDispatchAsync(
            ReopenDispatchCommand command,
            CancellationToken cancellationToken = default)
        {
            LastReopenCommand = command;
            return Task.FromResult(
                OperationResult<ReopenDispatchReceiptDto>.Success(
                    new ReopenDispatchReceiptDto(command.RequestId, "BG-2026-7101", "LTR-8001")));
        }

        public Task<OperationResult<DispatchLetterPdfResult>> GetLetterPdfAsync(Guid dispatcherUserId, Guid requestId, string referenceNumber, DateOnly letterDate, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
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
