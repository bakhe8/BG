namespace BG.Application.Contracts.Services;

public interface INotificationBroadcaster
{
    Task BroadcastNotificationAsync(string message, string? link, string requiredPermission, DateTimeOffset createdAtUtc, Guid? targetUserId = null, CancellationToken cancellationToken = default);
}
