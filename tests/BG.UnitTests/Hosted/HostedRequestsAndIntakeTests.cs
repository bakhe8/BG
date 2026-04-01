using System.Net;
using BG.Domain.Guarantees;
using Microsoft.EntityFrameworkCore;

namespace BG.UnitTests.Hosted;

public sealed partial class HostedFlowTests
{
    [Fact]
    public async Task Intake_workspace_loads_minimal_execution_surface_for_signed_in_actor()
    {
        await using var factory = new HostedAppFactory();
        var client = factory.CreateAppClient();

        var signInResponse = await client.PostFormWithAntiforgeryAsync(
            "/SignIn?returnUrl=%2FIntake%2FWorkspace",
            "/SignIn",
            new Dictionary<string, string?>
            {
                ["Username"] = "hosted.admin",
                ["Password"] = "HostedAdmin123!",
                ["ReturnUrl"] = "/Intake/Workspace"
            });

        Assert.Equal(HttpStatusCode.Redirect, signInResponse.StatusCode);
        Assert.Equal("/Intake/Workspace", signInResponse.Headers.Location?.OriginalString);

        var intakeResponse = await client.GetAsync("/Intake/Workspace");
        var intakeHtml = await intakeResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, intakeResponse.StatusCode);
        Assert.Contains("intake-surface-grid", intakeHtml, StringComparison.Ordinal);
        Assert.Contains("name=\"Input.UploadedDocument\"", intakeHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Request_submission_moves_request_into_approvals_queue_across_signed_in_users()
    {
        await using var factory = new HostedAppFactory();
        Guid requesterId = Guid.Empty;
        const string guaranteeNumber = "BG-2026-E2E-1001";

        await factory.ExecuteDbContextAsync(async (dbContext, serviceProvider) =>
        {
            var requester = await SeedLocalUserAsync(
                dbContext,
                serviceProvider,
                "e2e.requester",
                "E2E Requester",
                "E2ERequester123!",
                ("E2E Requester Role", ["requests.view", "requests.create"]));
            var approvalRole = await EnsureRoleAsync(
                dbContext,
                "E2E Approval Role",
                ["approvals.queue.view", "approvals.sign"]);
            _ = await SeedLocalUserAsync(
                dbContext,
                serviceProvider,
                "e2e.approver",
                "E2E Approver",
                "E2EApprover123!",
                ("E2E Approval Role", ["approvals.queue.view", "approvals.sign"]));

            var guarantee = Guarantee.RegisterNew(
                guaranteeNumber,
                "National Bank",
                "KFSHRC",
                "Main Contractor",
                GuaranteeCategory.Contract,
                150000m,
                "SAR",
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 12, 31),
                DateTimeOffset.UtcNow);
            await dbContext.Guarantees.AddAsync(guarantee);

            var definition = await dbContext.RequestWorkflowDefinitions
                .Include(item => item.Stages)
                .SingleAsync(item => item.RequestType == GuaranteeRequestType.Extend && item.GuaranteeCategory == null);

            foreach (var stage in definition.Stages.OrderBy(stage => stage.Sequence))
            {
                definition.UpdateStage(stage.Id, approvalRole.Id, customTitle: null, customSummary: null, DateTimeOffset.UtcNow);
            }

            requesterId = requester.Id;
        });

        var requesterClient = factory.CreateAppClient();
        var requesterLogin = await requesterClient.PostFormWithAntiforgeryAsync(
            "/SignIn?returnUrl=%2FRequests%2FWorkspace",
            "/SignIn",
            new Dictionary<string, string?>
            {
                ["Username"] = "e2e.requester",
                ["Password"] = "E2ERequester123!",
                ["ReturnUrl"] = "/Requests/Workspace"
            });
        Assert.Equal(HttpStatusCode.Redirect, requesterLogin.StatusCode);

        var createResponse = await requesterClient.PostFormWithAntiforgeryAsync(
            "/Requests/Workspace",
            "/Requests/Workspace",
            new Dictionary<string, string?>
            {
                ["Input.GuaranteeNumber"] = guaranteeNumber,
                ["Input.RequestType"] = GuaranteeRequestType.Extend.ToString(),
                ["Input.RequestedExpiryDate"] = "2027-12-31",
                ["Input.Notes"] = "Hosted flow extension"
            });

        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        Assert.StartsWith("/Requests/Workspace", createResponse.Headers.Location?.OriginalString, StringComparison.Ordinal);

        var requestId = await factory.QueryDbContextAsync(async dbContext =>
        {
            return await dbContext.GuaranteeRequests
                .Where(request => request.RequestedByUserId == requesterId && request.Guarantee.GuaranteeNumber == guaranteeNumber)
                .Select(request => request.Id)
                .SingleAsync();
        });

        var submitResponse = await requesterClient.PostFormWithAntiforgeryAsync(
            "/Requests/Workspace",
            "/Requests/Workspace?handler=Submit",
            new Dictionary<string, string?>
            {
                ["requestId"] = requestId.ToString(),
                ["actorId"] = requesterId.ToString(),
                ["page"] = "1"
            });

        Assert.Equal(HttpStatusCode.Redirect, submitResponse.StatusCode);
        Assert.StartsWith("/Requests/Workspace", submitResponse.Headers.Location?.OriginalString, StringComparison.Ordinal);

        var approverClient = factory.CreateAppClient();
        var approverLogin = await approverClient.PostFormWithAntiforgeryAsync(
            "/SignIn?returnUrl=%2FApprovals%2FQueue",
            "/SignIn",
            new Dictionary<string, string?>
            {
                ["Username"] = "e2e.approver",
                ["Password"] = "E2EApprover123!",
                ["ReturnUrl"] = "/Approvals/Queue"
            });
        Assert.Equal(HttpStatusCode.Redirect, approverLogin.StatusCode);

        var approvalsResponse = await approverClient.GetAsync("/Approvals/Queue");
        var approvalsHtml = await approvalsResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, approvalsResponse.StatusCode);
        Assert.Contains(guaranteeNumber, approvalsHtml, StringComparison.Ordinal);
        Assert.Contains("approval-surface-grid", approvalsHtml, StringComparison.Ordinal);
        Assert.Contains($"/Approvals/Dossier/{requestId}", approvalsHtml, StringComparison.Ordinal);

        var requestState = await factory.QueryDbContextAsync(
            dbContext => dbContext.GuaranteeRequests
                .Include(request => request.ApprovalProcess)
                .SingleAsync(request => request.Id == requestId));

        Assert.Equal(GuaranteeRequestStatus.InApproval, requestState.Status);
        Assert.NotNull(requestState.ApprovalProcess);
    }
}
