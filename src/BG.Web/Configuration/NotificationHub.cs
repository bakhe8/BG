using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using BG.Web.Security;
using System.Security.Claims;

namespace BG.Web.Configuration;

[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var permissions = Context.User?.FindAll("permission")
            .Select(c => c.Value)
            .ToArray() ?? [];

        foreach (var permission in permissions)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(permission));
        }

        await base.OnConnectedAsync();
    }

    public static string GetGroupName(string permission) => $"group-permission.{permission}";
}
