using BG.Application.Contracts.Services;
using Microsoft.AspNetCore.SignalR;

namespace BG.Web.Configuration;

public sealed class NotificationBroadcaster : INotificationBroadcaster
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationBroadcaster(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastNotificationAsync(
        string message,
        string? link,
        string requiredPermission,
        DateTimeOffset createdAtUtc,
        Guid? targetUserId = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new { message, link, createdAt = createdAtUtc };

        if (targetUserId.HasValue)
        {
            var userGroup = NotificationHub.GetUserGroupName(targetUserId.Value);
            await _hubContext.Clients.Group(userGroup).SendAsync("ReceiveNotification", payload, cancellationToken);
        }
        else
        {
            var groupName = NotificationHub.GetGroupName(requiredPermission);
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", payload, cancellationToken);
        }
    }
}
