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
        CancellationToken cancellationToken = default)
    {
        var groupName = NotificationHub.GetGroupName(requiredPermission);
        await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", new
        {
            message,
            link,
            createdAt = createdAtUtc
        }, cancellationToken);
    }
}
