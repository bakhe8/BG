using BG.Application.Common;
using BG.Application.Contracts.Services;
using BG.Application.Models.Dispatch;
using BG.Domain.Guarantees;
using BG.Web.Localization;
using BG.Web.Pages.Dispatch;
using BG.Web.Security;
using BG.Web.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace BG.UnitTests.Web;

public sealed class DispatchLetterPageTests
{
    [Fact]
    public async Task OnGetAsync_loads_letter_preview_for_actor()
    {
        var actorId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var model = new LetterModel(new StubDispatchWorkspaceService(), new PassThroughLocalizer())
        {
            ActorId = actorId,
            RequestId = requestId,
            ReferenceNumber = "LTR-5001",
            LetterDate = "2026-03-12"
        };

        var result = await model.OnGetAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.Letter);
        Assert.Equal(requestId, model.Letter!.RequestId);
        Assert.Equal("LTR-5001", model.Letter.ReferenceNumber);
    }

    [Fact]
    public async Task OnGetAsync_uses_locked_actor_from_authenticated_session()
    {
        var actorId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var service = new StubDispatchWorkspaceService();
        var model = new LetterModel(service, new PassThroughLocalizer())
        {
            ActorId = Guid.NewGuid(),
            RequestId = requestId
        };
        AttachAuthenticatedUser(model, actorId);

        var result = await model.OnGetAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.True(model.IsActorContextLocked);
        Assert.Equal(actorId, service.LastActorId);
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
        public Guid LastActorId { get; private set; }

        public Task<DispatchWorkspaceSnapshotDto> GetWorkspaceAsync(Guid? dispatcherActorId, int pageNumber = 1, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<OperationResult<DispatchLetterPreviewDto>> GetLetterPreviewAsync(
            Guid dispatcherUserId,
            Guid requestId,
            string? referenceNumber,
            string? letterDate,
            CancellationToken cancellationToken = default)
        {
            LastActorId = dispatcherUserId;

            return Task.FromResult(
                OperationResult<DispatchLetterPreviewDto>.Success(
                    new DispatchLetterPreviewDto(
                        requestId,
                        "BG-2026-5001",
                        "National Bank",
                        "KFSHRC",
                        "Prime Contractor",
                        "SAR",
                        120000m,
                        new DateOnly(2026, 1, 1),
                        new DateOnly(2026, 12, 31),
                        GuaranteeRequestType.Extend,
                        "Request Owner",
                        referenceNumber ?? "LTR-5001",
                        new DateOnly(2026, 3, 12),
                        null,
                        new DateOnly(2027, 6, 30),
                        "Ready for dispatch",
                        "Dispatch One",
                        DateTimeOffset.UtcNow,
                        true,
                        0)));
        }

        public Task<OperationResult<PrintDispatchLetterReceiptDto>> PrintDispatchLetterAsync(PrintDispatchLetterCommand command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<OperationResult<RecordDispatchReceiptDto>> RecordDispatchAsync(RecordDispatchCommand command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<OperationResult<ConfirmDispatchDeliveryReceiptDto>> ConfirmDeliveryAsync(ConfirmDispatchDeliveryCommand command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<OperationResult<ReopenDispatchReceiptDto>> ReopenDispatchAsync(ReopenDispatchCommand command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
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
