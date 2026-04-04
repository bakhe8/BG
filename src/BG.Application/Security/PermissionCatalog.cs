namespace BG.Application.Security;

public static class PermissionCatalog
{
    public static IReadOnlyList<PermissionDefinition> Definitions { get; } =
    [
        new("intake.view", "Intake"),
        new("intake.scan", "Intake"),
        new("intake.verify", "Intake"),
        new("intake.finalize", "Intake"),
        new("operations.queue.view", "Operations"),
        new("operations.queue.manage", "Operations"),
        new("requests.view", "Requests"),
        new("requests.create", "Requests"),
        new("approvals.queue.view", "Approvals"),
        new("approvals.sign", "Approvals"),
        new("workflow.manage", "Workflow"),
        new("dispatch.view", "Dispatch"),
        new("dispatch.print", "Dispatch"),
        new("dispatch.record", "Dispatch"),
        new("dispatch.email", "Dispatch"),
        new("users.view", "Administration"),
        new("users.manage", "Administration"),
        new("delegations.manage", "Administration"),
        new("roles.view", "Administration"),
        new("roles.manage", "Administration"),
        new("administration.banks", "Administration"),
        new("reports.view", "Reports")
    ];
}
