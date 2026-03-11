namespace BG.Application.Security;

public static class PermissionCatalog
{
    public static IReadOnlyList<PermissionDefinition> Definitions { get; } =
    [
        new("dashboard.view", "Dashboard"),
        new("intake.view", "Intake"),
        new("intake.scan", "Intake"),
        new("intake.verify", "Intake"),
        new("intake.finalize", "Intake"),
        new("dispatch.view", "Dispatch"),
        new("dispatch.print", "Dispatch"),
        new("dispatch.record", "Dispatch"),
        new("dispatch.email", "Dispatch"),
        new("users.view", "Administration"),
        new("users.manage", "Administration"),
        new("roles.view", "Administration"),
        new("roles.manage", "Administration"),
        new("guarantees.view", "Guarantees"),
        new("guarantees.manage", "Guarantees")
    ];
}
