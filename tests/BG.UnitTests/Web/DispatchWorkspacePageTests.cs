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
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Dispatch/Workspace", redirect.PageName);
        Assert.True(redirect.RouteValues is null || !redirect.RouteValues.ContainsKey("actor"));
        Assert.True(model.IsActorContextLocked);
        Assert.Equal(lockedActorId, service.LastReopenCommand!.DispatcherUserId);
        Assert.Equal("DispatchWorkspace_ReopenSuccess", model.StatusMessage);
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

        public StubDispatchWorkspaceService(Guid actorId)
        {
            _actorId = actorId;
        }

        public PrintDispatchLetterCommand? LastPrintCommand { get; private set; }

        public RecordDispatchCommand? LastDispatchCommand { get; private set; }

        public ConfirmDispatchDeliveryCommand? LastDeliveryCommand { get; private set; }

        public ReopenDispatchCommand? LastReopenCommand { get; private set; }

        public Task<DispatchWorkspaceSnapshotDto> GetWorkspaceAsync(
            Guid? dispatcherActorId,
            int pageNumber = 1,
            CancellationToken cancellationToken = default)
        {
            var actor = new DispatchActorSummaryDto(_actorId, "dispatch.one", "Dispatch One", true, true, false);

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
