using BG.Web.Pages.Administration;
using BG.Web.Security;
using Microsoft.AspNetCore.Authorization;

namespace BG.UnitTests.Web;

public sealed class WorkspaceAuthorizationPolicyTests
{
    [Theory]
    [InlineData(typeof(BG.Web.Pages.Intake.WorkspaceModel), PermissionPolicyNames.IntakeWorkspace)]
    [InlineData(typeof(BG.Web.Pages.Operations.QueueModel), PermissionPolicyNames.OperationsQueue)]
    [InlineData(typeof(BG.Web.Pages.Requests.WorkspaceModel), PermissionPolicyNames.RequestsWorkspace)]
    [InlineData(typeof(BG.Web.Pages.Approvals.QueueModel), PermissionPolicyNames.ApprovalsQueue)]
    [InlineData(typeof(BG.Web.Pages.Approvals.DossierModel), PermissionPolicyNames.ApprovalsQueue)]
    [InlineData(typeof(BG.Web.Pages.Dispatch.WorkspaceModel), PermissionPolicyNames.DispatchWorkspace)]
    [InlineData(typeof(UsersModel), PermissionPolicyNames.UsersManage)]
    [InlineData(typeof(RolesModel), PermissionPolicyNames.RolesManage)]
    [InlineData(typeof(DelegationsModel), PermissionPolicyNames.DelegationsManage)]
    [InlineData(typeof(WorkflowModel), PermissionPolicyNames.WorkflowManage)]
    public void Page_models_require_expected_authorization_policy(Type pageModelType, string expectedPolicy)
    {
        var attribute = Assert.Single(pageModelType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>());

        Assert.Equal(expectedPolicy, attribute.Policy);
    }
}
