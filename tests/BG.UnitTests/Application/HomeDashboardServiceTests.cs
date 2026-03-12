using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Models.Dashboard;
using BG.Application.Models.Identity;
using BG.Application.Services;
using BG.Domain.Guarantees;

namespace BG.UnitTests.Application;

public sealed class HomeDashboardServiceTests
{
    [Fact]
    public async Task GetSnapshotAsync_returns_anonymous_snapshot_when_user_is_not_authenticated()
    {
        var repository = new StubHomeDashboardRepository();
        var service = new HomeDashboardService(repository, new StubUserAccessProfileService());

        var snapshot = await service.GetSnapshotAsync(null);

        Assert.False(snapshot.IsAuthenticated);
        Assert.False(snapshot.HasVisibleOperationalAreas);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task GetSnapshotAsync_applies_permission_visibility_and_maps_intake_scenario_titles()
    {
        var userId = Guid.NewGuid();
        var repository = new StubHomeDashboardRepository
        {
            Snapshot = new HomeDashboardSnapshotDto(
                true,
                null,
                false,
                false,
                false,
                false,
                false,
                false,
                2,
                [],
                1,
                [],
                4,
                3,
                2,
                1,
                [],
                [
                    new HomeDashboardIntakeActivityDto(
                        Guid.NewGuid(),
                        "BG-2026-0001",
                        "capture.pdf",
                        GuaranteeDocumentType.BankResponse,
                        DateTimeOffset.UtcNow,
                        "Operator",
                        GuaranteeDocumentCaptureChannel.ManualUpload,
                        "extension-confirmation",
                        "OperationsReviewScenario_Unknown")
                ])
        };

        var service = new HomeDashboardService(
            repository,
            new StubUserAccessProfileService(
                new UserAccessProfileDto(
                    userId,
                    "dashboard.user",
                    "Dashboard User",
                    ["Requests"],
                    ["dashboard.view", "requests.view", "requests.create", "dispatch.view"])));

        var snapshot = await service.GetSnapshotAsync(userId);

        Assert.True(snapshot.IsAuthenticated);
        Assert.Equal("Dashboard User", snapshot.DisplayName);
        Assert.True(snapshot.CanViewRequests);
        Assert.True(snapshot.CanViewDispatch);
        Assert.True(snapshot.CanViewIntake);
        Assert.True(snapshot.CanViewExpiringGuarantees);
        Assert.False(snapshot.CanViewApprovals);
        Assert.False(snapshot.CanViewOperations);
        Assert.Equal("IntakeScenario_Extension_Title", Assert.Single(snapshot.RecentIntakeActivities).ScenarioTitleResourceKey);
        Assert.Equal(1, repository.Calls);
        Assert.NotNull(repository.LastQuery);
        Assert.True(repository.LastQuery!.IncludeRequests);
        Assert.True(repository.LastQuery.IncludeDispatch);
        Assert.True(repository.LastQuery.IncludeIntake);
        Assert.True(repository.LastQuery.IncludeExpiringGuarantees);
        Assert.False(repository.LastQuery.IncludeApprovals);
        Assert.False(repository.LastQuery.IncludeOperations);
    }

    private sealed class StubHomeDashboardRepository : IHomeDashboardRepository
    {
        public int Calls { get; private set; }

        public HomeDashboardQuery? LastQuery { get; private set; }

        public HomeDashboardSnapshotDto Snapshot { get; set; } = HomeDashboardSnapshotDto.Anonymous();

        public Task<HomeDashboardSnapshotDto> GetAuthenticatedDashboardAsync(
            HomeDashboardQuery query,
            CancellationToken cancellationToken = default)
        {
            Calls += 1;
            LastQuery = query;
            return Task.FromResult(Snapshot);
        }
    }

    private sealed class StubUserAccessProfileService : IUserAccessProfileService
    {
        private readonly UserAccessProfileDto? _profile;

        public StubUserAccessProfileService(UserAccessProfileDto? profile = null)
        {
            _profile = profile;
        }

        public Task<IReadOnlyList<WorkspaceUserOptionDto>> ListActiveUsersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<WorkspaceUserOptionDto>>([]);
        }

        public Task<UserAccessProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_profile);
        }
    }
}
