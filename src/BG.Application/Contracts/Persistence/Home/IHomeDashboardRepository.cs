using BG.Application.Models.Dashboard;

namespace BG.Application.Contracts.Persistence;

public interface IHomeDashboardRepository
{
    Task<HomeDashboardSnapshotDto> GetAuthenticatedDashboardAsync(
        HomeDashboardQuery query,
        CancellationToken cancellationToken = default);
}
