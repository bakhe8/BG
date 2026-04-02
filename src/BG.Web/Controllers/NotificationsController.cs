using BG.Application.Contracts.Services;
using BG.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BG.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var permissions = User.FindAll("permission").Select(c => c.Value).ToArray();

        var notifications = await _notificationService.GetUserNotificationsAsync(userId, permissions, cancellationToken);
        
        return Ok(notifications.Select(n => new
        {
            id = n.Id,
            message = n.Message,
            link = n.Link,
            createdAt = n.CreatedAtUtc,
            isRead = n.IsRead
        }));
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await _notificationService.MarkAsReadAsync(id, cancellationToken);
        return Ok();
    }
}
