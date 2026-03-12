using BG.Application.Models.Identity;

namespace BG.Web.UI;

public sealed record WorkspaceShellSnapshot(
    UserAccessProfileDto? CurrentUser,
    IReadOnlyList<WorkspaceUserOptionDto> AvailableUsers,
    IReadOnlyList<WorkspaceShellNavigationItem> NavigationItems);
