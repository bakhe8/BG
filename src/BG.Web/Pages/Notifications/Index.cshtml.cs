using BG.Application.Contracts.Services;
using BG.Domain.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BG.Web.Pages.Notifications;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly INotificationService _notificationService;

    public IndexModel(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public IReadOnlyList<Notification> Notifications { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
        var permissions = User.FindAll("bg.permission").Select(c => c.Value).ToArray();
        var result = await _notificationService.GetUserNotificationsAsync(userId, permissions, cancellationToken);
        Notifications = result.ToArray();
    }

    public async Task<IActionResult> OnPostMarkReadAsync(Guid id, CancellationToken cancellationToken)
    {
        await _notificationService.MarkAsReadAsync(id, cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllReadAsync(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
        var permissions = User.FindAll("bg.permission").Select(c => c.Value).ToArray();
        var items = await _notificationService.GetUserNotificationsAsync(userId, permissions, cancellationToken);
        foreach (var item in items)
            await _notificationService.MarkAsReadAsync(item.Id, cancellationToken);
        return RedirectToPage();
    }
}
