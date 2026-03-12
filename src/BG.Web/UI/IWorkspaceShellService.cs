namespace BG.Web.UI;

public interface IWorkspaceShellService
{
    Guid? GetAuthenticatedUserId();

    Task<WorkspaceShellSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task<bool> CurrentUserHasAnyPermissionAsync(IEnumerable<string> permissionKeys, CancellationToken cancellationToken = default);

    IReadOnlyList<string> GetRequiredPermissionKeys(PathString path);
}
