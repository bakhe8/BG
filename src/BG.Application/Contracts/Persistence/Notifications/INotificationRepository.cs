using BG.Domain.Notifications;

namespace BG.Application.Contracts.Persistence.Notifications;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, string[] userPermissions, CancellationToken cancellationToken = default);
    
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
