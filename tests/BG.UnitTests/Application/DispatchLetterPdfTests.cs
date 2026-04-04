using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Dispatch;
using BG.Application.Models.Approvals;
using BG.Application.Models.Dispatch;
using BG.Application.Services;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Notifications;
using BG.Domain.Workflow;

namespace BG.UnitTests.Application;

public sealed class DispatchLetterPdfTests
{
    [Fact]
    public async Task GetLetterPdfAsync_fails_when_request_not_ready_for_dispatch()
    {
        var fixture = Fixture.Create(readyForDispatch: false);
        var service = new DispatchWorkspaceService(
            fixture.Repository,
            new StubNotificationService(),
            new StubLetterGenerationService());

        var result = await service.GetLetterPdfAsync(
            fixture.Actor.Id,
            fixture.Request.Id,
            "LTR-7001",
            new DateOnly(2026, 3, 12));

        Assert.False(result.Succeeded);
        Assert.Equal(DispatchErrorCodes.RequestNotReady, result.ErrorCode);
    }

    [Fact]
    public async Task GetLetterPdfAsync_returns_pdf_bytes_for_ready_request()
    {
        var fixture = Fixture.Create(readyForDispatch: true);
        var service = new DispatchWorkspaceService(
            fixture.Repository,
            new StubNotificationService(),
            new StubLetterGenerationService());

        var result = await service.GetLetterPdfAsync(
            fixture.Actor.Id,
            fixture.Request.Id,
            "LTR-7002",
            new DateOnly(2026, 3, 12));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value.Content);
        Assert.EndsWith(".pdf", result.Value.FileName, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record Fixture(User Actor, GuaranteeRequest Request, StubDispatchWorkspaceRepository Repository)
    {
        public static Fixture Create(bool readyForDispatch)
        {
            var role = new Role("Dispatch", "Dispatch role");
            role.AssignPermissions([
                new Permission("dispatch.view", "Dispatch"),
                new Permission("dispatch.print", "Dispatch")
            ]);

            var actor = new User(
                "dispatch.user",
                "Dispatch User",
                "dispatch.user@example.com",
                null,
                UserSourceType.Local,
                true,
                DateTimeOffset.UtcNow);
            actor.AssignRoles([role]);

            var requester = new User(
                "request.user",
                "Request User",
                "request.user@example.com",
                null,
                UserSourceType.Local,
                true,
                DateTimeOffset.UtcNow);

            var guarantee = Guarantee.RegisterNew(
                "BG-2026-9001",
                "National Bank",
                "KFSHRC",
                "Prime Contractor",
                GuaranteeCategory.Contract,
                150000m,
                "SAR",
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 12, 31),
                DateTimeOffset.UtcNow);

            var request = guarantee.CreateRequest(
                requester.Id,
                GuaranteeRequestType.Extend,
                null,
                new DateOnly(2027, 1, 31),
                "phase4 test",
                DateTimeOffset.UtcNow,
                requester.DisplayName);
            request.RequestedByUser = requester;

            if (readyForDispatch)
            {
                var process = new RequestApprovalProcess(request.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);
                process.AddStage(role.Id, "WorkflowStage_GuaranteesSupervisor_Title", "WorkflowStage_GuaranteesSupervisor_Summary", null, null, true);
                process.Start();
                request.SubmitForApproval(process);
                process.ApproveCurrentStage(actor.Id, DateTimeOffset.UtcNow, "approved");
                request.MarkApprovedForDispatch();
            }

            var repository = new StubDispatchWorkspaceRepository(actor, request);
            return new Fixture(actor, request, repository);
        }
    }

    private sealed class StubDispatchWorkspaceRepository : IDispatchWorkspaceRepository
    {
        private readonly User _actor;
        private readonly GuaranteeRequest _request;

        public StubDispatchWorkspaceRepository(User actor, GuaranteeRequest request)
        {
            _actor = actor;
            _request = request;
        }

        public Task<IReadOnlyList<User>> ListDispatchActorsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<User>>([_actor]);

        public Task<User?> GetDispatchActorByIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult(userId == _actor.Id ? _actor : null);

        public Task<PagedResult<DispatchQueueItemReadModel>> ListReadyRequestsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<DispatchQueueItemReadModel>([], new PageInfoDto(pageNumber, pageSize, 0)));

        public Task<IReadOnlyList<DispatchPendingDeliveryItemReadModel>> ListPendingDeliveryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DispatchPendingDeliveryItemReadModel>>([]);

        public Task<GuaranteeRequest?> GetRequestForDispatchAsync(Guid requestId, CancellationToken cancellationToken = default)
            => Task.FromResult(requestId == _request.Id ? _request : null);

        public Task<IReadOnlyList<ApprovalPriorSignatureReadModel>> GetApprovalSignaturesForRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ApprovalPriorSignatureReadModel> signatures =
            [
                new ApprovalPriorSignatureReadModel(
                    Guid.NewGuid(),
                    1,
                    "WorkflowStage_GuaranteesSupervisor_Title",
                    "Guarantees Supervisor",
                    "Guarantees Supervisor",
                    DateTimeOffset.UtcNow,
                    _actor.Id,
                    _actor.DisplayName,
                    _actor.Id,
                    _actor.DisplayName)
            ];

            return Task.FromResult(requestId == _request.Id ? signatures : []);
        }

        public void TrackNewOutgoingCorrespondence(GuaranteeCorrespondence correspondence)
        {
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubLetterGenerationService : ILetterGenerationService
    {
        public Task<byte[]> GenerateLetterPdfAsync(DispatchLetterPreviewDto letter, IReadOnlyList<ApprovalPriorSignatureDto> signatures, CancellationToken cancellationToken = default)
            => Task.FromResult("PDF"u8.ToArray());
    }

    private sealed class StubNotificationService : INotificationService
    {
        public Task SendNotificationAsync(string message, string? link, string requiredPermission, Guid? targetUserId = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, string[] userPermissions, CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<Notification>>([]);

        public Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
