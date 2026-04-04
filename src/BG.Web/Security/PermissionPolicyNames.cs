using Microsoft.AspNetCore.Authorization;

namespace BG.Web.Security;

public static class PermissionPolicyNames
{
    public const string IntakeWorkspace = "permission.intake.workspace";
    public const string OperationsQueue = "permission.operations.queue";
    public const string RequestsWorkspace = "permission.requests.workspace";
    public const string GuaranteeHistory = "permission.requests.history";
    public const string ApprovalsQueue = "permission.approvals.queue";
    public const string DispatchWorkspace = "permission.dispatch.workspace";
    public const string UsersRead = "permission.users.read";
    public const string UsersManage = "permission.users.manage";
    public const string RolesRead = "permission.roles.read";
    public const string RolesManage = "permission.roles.manage";
    public const string DelegationsManage = "permission.delegations.manage";
    public const string WorkflowManage = "permission.workflow.manage";
    public const string BanksManage = "permission.administration.banks";
    public const string ReportsView = "permission.reports.view";

    public static void Configure(AuthorizationOptions options)
    {
        Add(options, IntakeWorkspace, "intake.view", "intake.scan", "intake.verify", "intake.finalize");
        Add(options, OperationsQueue, "operations.queue.view", "operations.queue.manage");
        Add(options, RequestsWorkspace, "requests.view", "requests.create");
        Add(options, GuaranteeHistory, "requests.view", "approvals.queue.view", "approvals.sign", "dispatch.view", "operations.queue.view");
        Add(options, ApprovalsQueue, "approvals.queue.view", "approvals.sign");
        Add(options, DispatchWorkspace, "dispatch.view", "dispatch.print", "dispatch.record", "dispatch.email");
        Add(options, UsersRead, "users.view", "users.manage");
        Add(options, UsersManage, "users.manage");
        Add(options, RolesRead, "roles.view", "roles.manage");
        Add(options, RolesManage, "roles.manage");
        Add(options, DelegationsManage, "delegations.manage");
        Add(options, WorkflowManage, "workflow.manage");
        Add(options, BanksManage, "administration.banks");
        Add(options, ReportsView, "reports.view");
    }

    private static void Add(AuthorizationOptions options, string policyName, params string[] permissionKeys)
    {
        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new PermissionAuthorizationRequirement(permissionKeys));
        });
    }
}
