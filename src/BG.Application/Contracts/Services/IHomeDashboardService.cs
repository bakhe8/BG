using BG.Application.Models.Dashboard;

namespace BG.Application.Contracts.Services;

public interface IHomeDashboardService
{
    Task<HomeDashboardSnapshotDto> GetSnapshotAsync(Guid? userId, CancellationToken cancellationToken = default);
}
