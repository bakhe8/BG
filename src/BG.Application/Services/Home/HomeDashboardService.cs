using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Models.Dashboard;

namespace BG.Application.Services;

internal sealed class HomeDashboardService : IHomeDashboardService
{
    private readonly IHomeDashboardRepository _repository;
    private readonly IUserAccessProfileService _userAccessProfileService;

    public HomeDashboardService(
        IHomeDashboardRepository repository,
        IUserAccessProfileService userAccessProfileService)
    {
        _repository = repository;
        _userAccessProfileService = userAccessProfileService;
    }

    public async Task<HomeDashboardSnapshotDto> GetSnapshotAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue)
        {
            return HomeDashboardSnapshotDto.Anonymous();
        }

        var profile = await _userAccessProfileService.GetProfileAsync(userId.Value, cancellationToken);
        if (profile is null)
        {
            return HomeDashboardSnapshotDto.Anonymous();
        }

        var permissions = profile.PermissionKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var canViewApprovals = HasAnyPermission(permissions, "approvals.queue.view", "approvals.sign");
        var canViewRequests = HasAnyPermission(permissions, "requests.view", "requests.create");
        var canViewOperations = HasAnyPermission(permissions, "operations.queue.view", "operations.queue.manage");
        var canViewDispatch = HasAnyPermission(permissions, "dispatch.view", "dispatch.print", "dispatch.record", "dispatch.email");
        var canViewIntake = HasAnyPermission(permissions, "intake.view", "intake.scan", "intake.verify", "intake.finalize");
        var canViewExpiringGuarantees = canViewApprovals || canViewRequests || canViewOperations || canViewDispatch;

        var snapshot = await _repository.GetAuthenticatedDashboardAsync(
            new HomeDashboardQuery(
                userId.Value,
                canViewApprovals,
                canViewRequests,
                canViewOperations,
                canViewDispatch,
                canViewIntake,
                canViewExpiringGuarantees,
                DateOnly.FromDateTime(DateTime.Now),
                DateTimeOffset.UtcNow),
            cancellationToken);

        return snapshot with
        {
            IsAuthenticated = true,
            DisplayName = profile.DisplayName,
            CanViewApprovals = canViewApprovals,
            CanViewRequests = canViewRequests,
            CanViewOperations = canViewOperations,
            CanViewDispatch = canViewDispatch,
            CanViewIntake = canViewIntake,
            CanViewExpiringGuarantees = canViewExpiringGuarantees,
            RecentIntakeActivities = snapshot.RecentIntakeActivities
                .Select(activity => activity with
                {
                    ScenarioTitleResourceKey = ResolveScenarioTitleResourceKey(activity.ScenarioKey)
                })
                .ToArray()
        };
    }

    private static bool HasAnyPermission(IReadOnlySet<string> permissions, params string[] permissionKeys)
    {
        return permissionKeys.Any(permissions.Contains);
    }

    private static string ResolveScenarioTitleResourceKey(string? scenarioKey)
    {
        return IntakeScenarioCatalog.Find(scenarioKey)?.TitleResourceKey ?? "OperationsReviewScenario_Unknown";
    }
}
