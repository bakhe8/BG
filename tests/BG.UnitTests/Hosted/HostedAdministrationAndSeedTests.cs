using System.Net;
using BG.Domain.Guarantees;
using BG.Domain.Operations;
using BG.Domain.Workflow;
using BG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BG.UnitTests.Hosted;

public sealed partial class HostedFlowTests
{
    [Fact]
    public async Task Workflow_administration_enforces_authorization_and_updates_stage_over_http()
    {
        await using var factory = new HostedAppFactory();
        var approverRoleId = Guid.Empty;
        var definitionId = Guid.Empty;
        var stageId = Guid.Empty;

        await factory.ExecuteDbContextAsync(async (dbContext, serviceProvider) =>
        {
            var requester = await SeedLocalUserAsync(
                dbContext,
                serviceProvider,
                "hosted.requester",
                "Hosted Requester",
                "HostedRequester123!",
                ("Hosted Requester Role", ["requests.view", "requests.create"]));

            var role = await EnsureRoleAsync(
                dbContext,
                "Hosted Approval Role",
                ["approvals.queue.view", "approvals.sign"]);

            var definition = new RequestWorkflowDefinition(
                "HostedCustomWorkflow",
                GuaranteeRequestType.Extend,
                GuaranteeCategory.Contract,
                "GuaranteeCategory_Contract",
                "WorkflowTemplate_Extend_Title",
                "WorkflowTemplate_Extend_Summary",
                DateTimeOffset.UtcNow);
            var stage = definition.AddStage(
                roleId: null,
                "WorkflowStage_GuaranteesSupervisor_Title",
                "WorkflowStage_GuaranteesSupervisor_Summary",
                customTitle: null,
                customSummary: null,
                requiresLetterSignature: true,
                modifiedAtUtc: DateTimeOffset.UtcNow);

            await dbContext.RequestWorkflowDefinitions.AddAsync(definition);

            _ = requester;
            approverRoleId = role.Id;
            definitionId = definition.Id;
            stageId = stage.Id;
        });

        var requesterClient = factory.CreateAppClient();
        var requesterSignIn = await requesterClient.PostFormWithAntiforgeryAsync(
            "/SignIn?returnUrl=%2FAdministration%2FWorkflow",
            "/SignIn",
            new Dictionary<string, string?>
            {
                ["Username"] = "hosted.requester",
                ["Password"] = "HostedRequester123!",
                ["ReturnUrl"] = "/Administration/Workflow"
            });

        Assert.Equal(HttpStatusCode.Redirect, requesterSignIn.StatusCode);

        var forbiddenResponse = await requesterClient.GetAsync("/Administration/Workflow");
        Assert.Equal(HttpStatusCode.Redirect, forbiddenResponse.StatusCode);
        Assert.Equal("/?shellMessage=WorkspaceShell_AccessDenied", forbiddenResponse.Headers.Location?.OriginalString);

        var adminClient = factory.CreateAppClient();
        var adminSignIn = await adminClient.PostFormWithAntiforgeryAsync(
            "/SignIn?returnUrl=%2FAdministration%2FWorkflow",
            "/SignIn",
            new Dictionary<string, string?>
            {
                ["Username"] = "hosted.admin",
                ["Password"] = "HostedAdmin123!",
                ["ReturnUrl"] = "/Administration/Workflow"
            });

        Assert.Equal(HttpStatusCode.Redirect, adminSignIn.StatusCode);

        var updateResponse = await adminClient.PostFormWithAntiforgeryAsync(
            "/Administration/Workflow",
            "/Administration/Workflow?handler=UpdateStage",
            new Dictionary<string, string?>
            {
                ["definitionId"] = definitionId.ToString(),
                ["stageId"] = stageId.ToString(),
                ["roleId"] = approverRoleId.ToString(),
                ["customTitle"] = "Hosted Approval Stage",
                ["customSummary"] = "Configured over HTTP",
                ["delegationPolicy"] = "DirectSignerRequired"
            });

        Assert.Equal(HttpStatusCode.Redirect, updateResponse.StatusCode);
        Assert.NotNull(updateResponse.Headers.Location);
        Assert.Equal(
            $"/Administration/Workflow?definition={definitionId:D}",
            updateResponse.Headers.Location!.OriginalString);

        var workflowDefinition = await factory.QueryDbContextAsync(
            dbContext => dbContext.RequestWorkflowDefinitions
                .Include(definition => definition.Stages)
                .SingleAsync(definition => definition.Id == definitionId));

        var updatedStage = Assert.Single(workflowDefinition.Stages);
        Assert.Equal(approverRoleId, updatedStage.RoleId);
        Assert.True(workflowDefinition.IsActive);
        Assert.Equal("Hosted Approval Stage", updatedStage.CustomTitle);
        Assert.Equal(ApprovalDelegationPolicy.DirectSignerRequired, updatedStage.DelegationPolicy);
    }

    [Fact]
    public async Task Operational_seed_pack_creates_realistic_users_workflows_and_scenarios_idempotently()
    {
        await using var factory = new HostedAppFactory(
            new Dictionary<string, string?>
            {
                ["OperationalSeed:Enabled"] = "true",
                ["OperationalSeed:SharedPassword"] = "HostedSeedPassword!2026"
            });

        await factory.Services.InitializeInfrastructureAsync();

        var seededSnapshot = await factory.QueryDbContextAsync(async dbContext =>
        {
            var seededUsernames = await dbContext.Users
                .Where(user => user.Username.StartsWith("intake.") ||
                               user.Username.StartsWith("operations.") ||
                               user.Username.StartsWith("request.") ||
                               user.Username.StartsWith("dispatch.") ||
                               user.Username.StartsWith("guarantees.") ||
                               user.Username.StartsWith("department.") ||
                               user.Username.StartsWith("program.") ||
                               user.Username.StartsWith("deputy.") ||
                               user.Username.StartsWith("contracts.") ||
                               user.Username.StartsWith("procurement.") ||
                               user.Username.StartsWith("executive."))
                .OrderBy(user => user.Username)
                .Select(user => user.Username)
                .ToArrayAsync();

            var seededGuarantees = await dbContext.Guarantees
                .Where(guarantee => guarantee.GuaranteeNumber.StartsWith("BG-SEED-"))
                .OrderBy(guarantee => guarantee.GuaranteeNumber)
                .Select(guarantee => guarantee.GuaranteeNumber)
                .ToArrayAsync();

            var activeWorkflowCount = await dbContext.RequestWorkflowDefinitions
                .CountAsync(definition => definition.IsActive);

            var workflowStagesWithoutRoleCount = await dbContext.RequestWorkflowStages
                .CountAsync(stage => stage.RoleId == null);

            var pendingOperationsCount = await dbContext.OperationsReviewItems
                .CountAsync(item => item.Status != OperationsReviewItemStatus.Completed);

            return new
            {
                SeededUsernames = seededUsernames,
                SeededGuarantees = seededGuarantees,
                ActiveWorkflowCount = activeWorkflowCount,
                WorkflowStagesWithoutRoleCount = workflowStagesWithoutRoleCount,
                PendingOperationsCount = pendingOperationsCount
            };
        });

        Assert.Contains("intake.operator", seededSnapshot.SeededUsernames);
        Assert.Contains("request.owner1", seededSnapshot.SeededUsernames);
        Assert.Contains("dispatch.officer", seededSnapshot.SeededUsernames);
        Assert.Contains("executive.vp", seededSnapshot.SeededUsernames);

        Assert.Contains("BG-SEED-CT-0001", seededSnapshot.SeededGuarantees);
        Assert.Contains("BG-SEED-CT-0004", seededSnapshot.SeededGuarantees);
        Assert.Contains("BG-SEED-PO-0005", seededSnapshot.SeededGuarantees);
        Assert.Contains("BG-SEED-CT-0008", seededSnapshot.SeededGuarantees);
        Assert.Equal(8, seededSnapshot.SeededGuarantees.Length);
        Assert.True(seededSnapshot.ActiveWorkflowCount > 0);
        Assert.Equal(0, seededSnapshot.WorkflowStagesWithoutRoleCount);
        Assert.True(seededSnapshot.PendingOperationsCount >= 2);
    }
}
