using BG.Application.Contracts.Persistence.Notifications;
using BG.Application.Contracts.Services;
using BG.Domain.Notifications;

namespace BG.Application.Services;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly INotificationBroadcaster _broadcaster;

    public NotificationService(INotificationRepository repository, INotificationBroadcaster broadcaster)
    {
        _repository = repository;
        _broadcaster = broadcaster;
    }

    public async Task SendNotificationAsync(
        string message, 
        string? link, 
        string requiredPermission, 
        Guid? targetUserId = null, 
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification(
            message,
            link,
            requiredPermission,
            DateTimeOffset.UtcNow,
            targetUserId);

        await _repository.AddAsync(notification, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        // Broadcast to the permission group using the application-level broadcaster
        await _broadcaster.BroadcastNotificationAsync(
            notification.Message,
            notification.Link,
            notification.RequiredPermission,
            notification.CreatedAtUtc,
            notification.TargetUserId,
            cancellationToken);
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(
        Guid userId, 
        string[] userPermissions, 
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetUserNotificationsAsync(userId, userPermissions, cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _repository.GetByIdAsync(notificationId, cancellationToken);
        if (notification != null)
        {
            notification.MarkAsRead(DateTimeOffset.UtcNow);
            await _repository.SaveChangesAsync(cancellationToken);
        }
    }
}
