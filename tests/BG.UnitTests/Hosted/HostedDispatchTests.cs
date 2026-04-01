using System.Net;
using BG.Domain.Guarantees;
using BG.Domain.Workflow;
using Microsoft.EntityFrameworkCore;

namespace BG.UnitTests.Hosted;

public sealed partial class HostedFlowTests
{
    [Fact]
    public async Task Dispatch_workspace_loads_ready_requests_for_signed_in_actor()
    {
        await using var factory = new HostedAppFactory();
        const string guaranteeNumber = "BG-2026-DIS-1001";
        Guid requestId = Guid.Empty;

        await factory.ExecuteDbContextAsync(async (dbContext, serviceProvider) =>
        {
            var dispatcher = await dbContext.Users
                .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
                .ThenInclude(role => role.RolePermissions)
                .SingleAsync(user => user.Username == "hosted.admin");
            var dispatchRole = dispatcher.UserRoles
                .Select(userRole => userRole.Role)
                .First(role => role.RolePermissions.Any(permission =>
                    permission.PermissionKey == "dispatch.view" ||
                    permission.PermissionKey == "dispatch.print"));

            var requester = await SeedLocalUserAsync(
                dbContext,
                serviceProvider,
                "dispatch.requester",
                "Dispatch Requester",
                "DispatchRequester123!",
                ("Dispatch Requester Role", ["requests.view", "requests.create"]));

            var guarantee = Guarantee.RegisterNew(
                guaranteeNumber,
                "National Bank",
                "KFSHRC",
                "Main Contractor",
                GuaranteeCategory.Contract,
                90000m,
                "SAR",
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 12, 31),
                DateTimeOffset.UtcNow);

            var request = guarantee.CreateRequest(
                requester.Id,
                GuaranteeRequestType.Extend,
                requestedAmount: null,
                requestedExpiryDate: new DateOnly(2027, 1, 31),
                notes: "Dispatch hosted test",
                DateTimeOffset.UtcNow,
                requester.DisplayName);

            var workflowDefinition = await dbContext.RequestWorkflowDefinitions
                .SingleAsync(definition => definition.RequestType == GuaranteeRequestType.Extend && definition.GuaranteeCategory == null);

            var process = new RequestApprovalProcess(request.Id, workflowDefinition.Id, DateTimeOffset.UtcNow);
            process.AddStage(
                dispatchRole.Id,
                "WorkflowStage_GuaranteesSupervisor_Title",
                "WorkflowStage_GuaranteesSupervisor_Summary",
                titleText: null,
                summaryText: null,
                requiresLetterSignature: true);
            process.Start();
            request.SubmitForApproval(process);
            process.ApproveCurrentStage(dispatcher.Id, DateTimeOffset.UtcNow, note: null);
            request.MarkApprovedForDispatch();
            requestId = request.Id;

            await dbContext.Guarantees.AddAsync(guarantee);
        });

        var client = factory.CreateAppClient();
        var signInResponse = await client.PostFormWithAntiforgeryAsync(
            "/SignIn?returnUrl=%2FDispatch%2FWorkspace",
            "/SignIn",
            new Dictionary<string, string?>
            {
                ["Username"] = "hosted.admin",
                ["Password"] = "HostedAdmin123!",
                ["ReturnUrl"] = "/Dispatch/Workspace"
            });

        Assert.Equal(HttpStatusCode.Redirect, signInResponse.StatusCode);
        Assert.Equal("/Dispatch/Workspace", signInResponse.Headers.Location?.OriginalString);

        var dispatchResponse = await client.GetAsync("/Dispatch/Workspace");
        var dispatchHtml = await dispatchResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, dispatchResponse.StatusCode);
        Assert.True(
            dispatchHtml.Contains("dispatch-surface-grid", StringComparison.Ordinal) ||
            dispatchHtml.Contains("dispatch-surface-solo", StringComparison.Ordinal));
        Assert.Contains(guaranteeNumber, dispatchHtml, StringComparison.Ordinal);
        Assert.Contains("Dispatch Requester", dispatchHtml, StringComparison.Ordinal);

        var letterResponse = await client.GetAsync($"/Dispatch/Letter?requestId={requestId}&referenceNumber=LTR-1001&letterDate=2026-03-12");
        var letterHtml = await letterResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, letterResponse.StatusCode);
        Assert.Contains("LTR-1001", letterHtml, StringComparison.Ordinal);
        Assert.Contains(guaranteeNumber, letterHtml, StringComparison.Ordinal);
        Assert.Contains("window.print()", letterHtml, StringComparison.Ordinal);
    }
}
