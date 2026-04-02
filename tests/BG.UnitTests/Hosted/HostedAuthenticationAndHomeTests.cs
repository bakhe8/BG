using System.Net;
using BG.Application.Intake;
using BG.Domain.Guarantees;
using BG.Domain.Workflow;
using Microsoft.EntityFrameworkCore;

namespace BG.UnitTests.Hosted;

public sealed partial class HostedFlowTests
{
    [Fact]
    public async Task Protected_workspace_redirects_to_sign_in_and_sign_in_returns_to_requested_page()
    {
        await using var factory = new HostedAppFactory();
        var client = factory.CreateAppClient();

        var protectedResponse = await client.GetAsync("/Requests/Workspace");

        Assert.Equal(HttpStatusCode.Redirect, protectedResponse.StatusCode);
        Assert.Equal(
            "/SignIn?returnUrl=%2FRequests%2FWorkspace&shellMessage=WorkspaceShell_SignInRequired",
            protectedResponse.Headers.Location?.OriginalString);

        var signInResponse = await client.PostFormWithAntiforgeryAsync(
            "/SignIn?returnUrl=%2FRequests%2FWorkspace",
            "/SignIn",
            new Dictionary<string, string?>
            {
                ["Username"] = "hosted.admin",
                ["Password"] = "HostedAdmin123!",
                ["ReturnUrl"] = "/Requests/Workspace"
            });

        Assert.Equal(HttpStatusCode.Redirect, signInResponse.StatusCode);
        Assert.Equal("/Requests/Workspace", signInResponse.Headers.Location?.OriginalString);

        var workspaceResponse = await client.GetAsync("/Requests/Workspace");
        var workspaceHtml = await workspaceResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, workspaceResponse.StatusCode);
        Assert.Contains("requests-create-column", workspaceHtml, StringComparison.Ordinal);
        Assert.Contains("name=\"Input.GuaranteeNumber\"", workspaceHtml, StringComparison.Ordinal);
        Assert.Contains("Hosted Admin", workspaceHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Signed_in_home_page_shows_operational_dashboard_instead_of_framework_metadata()
    {
        await using var factory = new HostedAppFactory();
        const string guaranteeNumber = "BG-2026-DASH-1001";

        await factory.ExecuteDbContextAsync(async (dbContext, _) =>
        {
            var admin = await dbContext.Users
                .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
                .SingleAsync(user => user.Username == "hosted.admin");
            var adminRole = await dbContext.Roles.SingleAsync(role => role.Name == "System Administrators");
            var workflowDefinition = await dbContext.RequestWorkflowDefinitions
                .SingleAsync(definition => definition.RequestType == GuaranteeRequestType.Extend && definition.GuaranteeCategory == null);

            var guarantee = Guarantee.RegisterNew(
                guaranteeNumber,
                "National Bank",
                "KFSHRC",
                "Main Contractor",
                GuaranteeCategory.Contract,
                125000m,
                "SAR",
                new DateOnly(2026, 1, 1),
                DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(12)),
                DateTimeOffset.UtcNow);

            guarantee.RegisterScannedDocument(
                GuaranteeDocumentType.BankResponse,
                "dashboard-response.pdf",
                "/hosted/dashboard-response.pdf",
                1,
                DateTimeOffset.UtcNow,
                admin.Id,
                admin.DisplayName,
                GuaranteeDocumentCaptureChannel.ManualUpload,
                intakeScenarioKey: IntakeScenarioKeys.ExtensionConfirmation);

            var request = guarantee.CreateRequest(
                admin.Id,
                GuaranteeRequestType.Extend,
                requestedAmount: null,
                requestedExpiryDate: new DateOnly(2027, 1, 31),
                notes: "Dashboard visibility test",
                DateTimeOffset.UtcNow,
                admin.DisplayName);

            var process = new RequestApprovalProcess(request.Id, workflowDefinition.Id, DateTimeOffset.UtcNow);
            process.AddStage(
                adminRole.Id,
                "WorkflowStage_GuaranteesSupervisor_Title",
                "WorkflowStage_GuaranteesSupervisor_Summary",
                titleText: null,
                summaryText: null,
                requiresLetterSignature: true);
            process.Start();
            request.SubmitForApproval(process);

            await dbContext.Guarantees.AddAsync(guarantee);
        });

        var client = factory.CreateAppClient();
        var signInResponse = await client.PostFormWithAntiforgeryAsync(
            "/SignIn?returnUrl=%2F",
            "/SignIn",
            new Dictionary<string, string?>
            {
                ["Username"] = "hosted.admin",
                ["Password"] = "HostedAdmin123!",
                ["ReturnUrl"] = "/"
            });

        Assert.Equal(HttpStatusCode.Redirect, signInResponse.StatusCode);
        Assert.Equal("/", signInResponse.Headers.Location?.OriginalString);

        var homeResponse = await client.GetAsync("/");
        var homeHtml = await homeResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);
        Assert.Contains("home-primary-work-card", homeHtml, StringComparison.Ordinal);
        Assert.Contains("home-workspace-card-primary", homeHtml, StringComparison.Ordinal);
        Assert.Contains("/Approvals/Queue", homeHtml, StringComparison.Ordinal);
        Assert.DoesNotContain(guaranteeNumber, homeHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("Architecture decisions", homeHtml, StringComparison.OrdinalIgnoreCase);
    }
}
