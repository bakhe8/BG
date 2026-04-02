using BG.Domain.Notifications;

namespace BG.Application.Contracts.Services;

public interface INotificationService
{
    Task SendNotificationAsync(string message, string? link, string requiredPermission, Guid? targetUserId = null, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, string[] userPermissions, CancellationToken cancellationToken = default);
    
    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
}
