using System.Net;
using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Security;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Operations;
using BG.Domain.Workflow;
using BG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BG.UnitTests.Hosted;

public sealed class HostedFlowTests
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
        Assert.Contains("request-workspace-grid", workspaceHtml, StringComparison.Ordinal);
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
        Assert.Contains("home-workspace-grid", homeHtml, StringComparison.Ordinal);
        Assert.Contains(guaranteeNumber, homeHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("Architecture decisions", homeHtml, StringComparison.OrdinalIgnoreCase);
    }

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
    public async Task Operations_queue_loads_open_review_items_for_signed_in_actor()
    {
        await using var factory = new HostedAppFactory();
        const string guaranteeNumber = "BG-2026-OPS-1001";

        await factory.ExecuteDbContextAsync(async (dbContext, _) =>
        {
            var guarantee = Guarantee.RegisterNew(
                guaranteeNumber,
                "National Bank",
                "KFSHRC",
                "Main Contractor",
                GuaranteeCategory.Contract,
                80000m,
                "SAR",
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 12, 31),
                DateTimeOffset.UtcNow);
            var document = new GuaranteeDocument(
                guarantee.Id,
                GuaranteeDocumentType.BankResponse,
                GuaranteeDocumentSourceType.Uploaded,
                "bank-response.pdf",
                "/hosted/bank-response.pdf",
                1,
                DateTimeOffset.UtcNow,
                captureChannel: GuaranteeDocumentCaptureChannel.ManualUpload,
                intakeScenarioKey: IntakeScenarioKeys.ExtensionConfirmation,
                verifiedDataJson: "{}");
            var reviewItem = new OperationsReviewItem(
                guarantee.Id,
                guarantee.GuaranteeNumber,
                document.Id,
                guaranteeCorrespondenceId: null,
                IntakeScenarioKeys.ExtensionConfirmation,
                OperationsReviewItemCategory.IncomingBankConfirmation,
                DateTimeOffset.UtcNow);

            await dbContext.Guarantees.AddAsync(guarantee);
            await dbContext.GuaranteeDocuments.AddAsync(document);
            await dbContext.OperationsReviewItems.AddAsync(reviewItem);
        });

        var client = factory.CreateAppClient();
        var signInResponse = await client.PostFormWithAntiforgeryAsync(
            "/SignIn?returnUrl=%2FOperations%2FQueue",
            "/SignIn",
            new Dictionary<string, string?>
            {
                ["Username"] = "hosted.admin",
                ["Password"] = "HostedAdmin123!",
                ["ReturnUrl"] = "/Operations/Queue"
            });

        Assert.Equal(HttpStatusCode.Redirect, signInResponse.StatusCode);
        Assert.Equal("/Operations/Queue", signInResponse.Headers.Location?.OriginalString);

        var queueResponse = await client.GetAsync("/Operations/Queue");
        var queueHtml = await queueResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        Assert.Contains("operations-surface-grid", queueHtml, StringComparison.Ordinal);
        Assert.Contains(guaranteeNumber, queueHtml, StringComparison.Ordinal);
        Assert.Contains("bank-response.pdf", queueHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Operations_queue_shows_blocked_apply_reason_when_bank_profile_conflicts()
    {
        await using var factory = new HostedAppFactory();
        const string guaranteeNumber = "BG-2026-OPS-BLOCK-1001";
        var reviewItemId = Guid.Empty;

        await factory.ExecuteDbContextAsync(async (dbContext, _) =>
        {
            var operationsActor = await dbContext.Users
                .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
                .SingleAsync(user => user.Username == "hosted.admin");
            var workflowDefinition = await dbContext.RequestWorkflowDefinitions
                .SingleAsync(definition =>
                    definition.RequestType == GuaranteeRequestType.Extend &&
                    definition.GuaranteeCategory == null);

            var guarantee = Guarantee.RegisterNew(
                guaranteeNumber,
                "National Bank",
                "KFSHRC",
                "Main Contractor",
                GuaranteeCategory.Contract,
                82000m,
                "SAR",
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 12, 31),
                DateTimeOffset.UtcNow);

            var request = guarantee.CreateRequest(
                operationsActor.Id,
                GuaranteeRequestType.Extend,
                requestedAmount: null,
                requestedExpiryDate: new DateOnly(2027, 11, 30),
                notes: "Hosted operations blocked flow",
                DateTimeOffset.UtcNow.AddDays(-10),
                operationsActor.DisplayName);
            AttachRequestSourceDocument(
                guarantee,
                request,
                "riyad-instrument.pdf",
                "guarantee-instrument-riyad",
                "Riyad Bank");
            ApproveAndDispatchRequest(
                request,
                guarantee,
                workflowDefinition.Id,
                operationsActor.UserRoles.Select(userRole => userRole.Role.Id).First(),
                operationsActor.Id,
                "LTR-OPS-BLOCK-1",
                new DateOnly(2026, 3, 11));

            var document = guarantee.RegisterScannedDocument(
                GuaranteeDocumentType.BankResponse,
                "blocked-response.pdf",
                "/hosted/blocked-response.pdf",
                1,
                DateTimeOffset.UtcNow,
                operationsActor.Id,
                operationsActor.DisplayName,
                GuaranteeDocumentCaptureChannel.ManualUpload,
                intakeScenarioKey: IntakeScenarioKeys.ExtensionConfirmation,
                extractionMethod: "OCR",
                verifiedDataJson: "{\"documentFormKey\":\"bank-letter-snb\",\"bankName\":\"Saudi National Bank\",\"newExpiryDate\":\"2027-11-30\"}");

            var correspondence = guarantee.RegisterCorrespondence(
                requestId: null,
                GuaranteeCorrespondenceDirection.Incoming,
                GuaranteeCorrespondenceKind.BankConfirmation,
                "BNK-OPS-BLOCK-1",
                new DateOnly(2026, 3, 13),
                document.Id,
                "Bank extension confirmation",
                DateTimeOffset.UtcNow);

            var reviewItem = new OperationsReviewItem(
                guarantee.Id,
                guarantee.GuaranteeNumber,
                document.Id,
                correspondence.Id,
                IntakeScenarioKeys.ExtensionConfirmation,
                OperationsReviewItemCategory.IncomingBankConfirmation,
                DateTimeOffset.UtcNow.AddMinutes(-2));

            await dbContext.Guarantees.AddAsync(guarantee);
            await dbContext.OperationsReviewItems.AddAsync(reviewItem);

            reviewItemId = reviewItem.Id;
        });

        var client = factory.CreateAppClient();
        var signInResponse = await client.PostFormWithAntiforgeryAsync(
            "/SignIn?returnUrl=%2FOperations%2FQueue",
            "/SignIn",
            new Dictionary<string, string?>
            {
                ["Username"] = "hosted.admin",
                ["Password"] = "HostedAdmin123!",
                ["ReturnUrl"] = "/Operations/Queue"
            });

        Assert.Equal(HttpStatusCode.Redirect, signInResponse.StatusCode);
        Assert.Equal("/Operations/Queue", signInResponse.Headers.Location?.OriginalString);

        var queueResponse = await client.GetAsync($"/Operations/Queue?item={reviewItemId}");
        var queueHtml = await queueResponse.Content.ReadAsStringAsync();
        var decodedQueueHtml = WebUtility.HtmlDecode(queueHtml);

        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        Assert.Contains("operations-surface-grid", queueHtml, StringComparison.Ordinal);
        Assert.Contains(guaranteeNumber, queueHtml, StringComparison.Ordinal);
        Assert.Contains("محجوب", decodedQueueHtml, StringComparison.Ordinal);
        Assert.Contains("عائلة بنك مختلفة", decodedQueueHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("تطبيق رد البنك", decodedQueueHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Operations_queue_can_reopen_an_applied_bank_response_over_http()
    {
        await using var factory = new HostedAppFactory();
        const string guaranteeNumber = "BG-2026-OPS-REOPEN-1001";
        var reviewItemId = Guid.Empty;
        var requestId = Guid.Empty;
        var correspondenceId = Guid.Empty;
        var operationsActorId = Guid.Empty;
        var originalExpiryDate = default(DateOnly);

        await factory.ExecuteDbContextAsync(async (dbContext, _) =>
        {
            var operationsActor = await dbContext.Users
                .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
                .SingleAsync(user => user.Username == "hosted.admin");
            var workflowDefinition = await dbContext.RequestWorkflowDefinitions
                .SingleAsync(definition =>
                    definition.RequestType == GuaranteeRequestType.Extend &&
                    definition.GuaranteeCategory == null);

            operationsActorId = operationsActor.Id;

            var guarantee = Guarantee.RegisterNew(
                guaranteeNumber,
                "National Bank",
                "KFSHRC",
                "Main Contractor",
                GuaranteeCategory.Contract,
                81000m,
                "SAR",
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 12, 31),
                DateTimeOffset.UtcNow);
            originalExpiryDate = guarantee.ExpiryDate;

            var request = guarantee.CreateRequest(
                operationsActor.Id,
                GuaranteeRequestType.Extend,
                requestedAmount: null,
                requestedExpiryDate: new DateOnly(2027, 10, 31),
                notes: "Hosted operations reopen flow",
                DateTimeOffset.UtcNow.AddDays(-10),
                operationsActor.DisplayName);
            ApproveAndDispatchRequest(
                request,
                guarantee,
                workflowDefinition.Id,
                operationsActor.UserRoles.Select(userRole => userRole.Role.Id).First(),
                operationsActor.Id,
                "LTR-OPS-REOPEN-1",
                new DateOnly(2026, 3, 11));

            var document = guarantee.RegisterScannedDocument(
                GuaranteeDocumentType.BankResponse,
                "reopen-response.pdf",
                "/hosted/reopen-response.pdf",
                1,
                DateTimeOffset.UtcNow,
                operationsActor.Id,
                operationsActor.DisplayName,
                GuaranteeDocumentCaptureChannel.ManualUpload,
                intakeScenarioKey: IntakeScenarioKeys.ExtensionConfirmation,
                extractionMethod: "OCR",
                verifiedDataJson: "{\"newExpiryDate\":\"2027-10-31\"}");

            var correspondence = guarantee.RegisterCorrespondence(
                requestId: null,
                GuaranteeCorrespondenceDirection.Incoming,
                GuaranteeCorrespondenceKind.BankConfirmation,
                "BNK-OPS-REOPEN-1",
                new DateOnly(2026, 3, 13),
                document.Id,
                "Bank extension confirmation",
                DateTimeOffset.UtcNow);

            guarantee.ApplyBankConfirmation(
                request.Id,
                correspondence.Id,
                DateTimeOffset.UtcNow,
                confirmedExpiryDate: new DateOnly(2027, 10, 31),
                notes: "Applied before hosted reopen test.",
                actedByUserId: operationsActor.Id,
                actedByDisplayName: operationsActor.DisplayName,
                operationsScenarioTitleResourceKey: "IntakeScenario_Extension_Title",
                operationsLaneResourceKey: "OperationsReviewLane_BankConfirmationReview",
                operationsMatchConfidenceResourceKey: "OperationsMatchConfidence_High",
                operationsMatchScore: 91,
                operationsPolicyResourceKey: "OperationsLedgerPolicy_MatchedSuggestionApplied");

            var reviewItem = new OperationsReviewItem(
                guarantee.Id,
                guarantee.GuaranteeNumber,
                document.Id,
                correspondence.Id,
                IntakeScenarioKeys.ExtensionConfirmation,
                OperationsReviewItemCategory.IncomingBankConfirmation,
                DateTimeOffset.UtcNow.AddMinutes(-2));
            reviewItem.MarkCompleted(DateTimeOffset.UtcNow.AddMinutes(-1));

            await dbContext.Guarantees.AddAsync(guarantee);
            await dbContext.OperationsReviewItems.AddAsync(reviewItem);

            reviewItemId = reviewItem.Id;
            requestId = request.Id;
            correspondenceId = correspondence.Id;
        });

        var client = factory.CreateAppClient();
        var signInResponse = await client.PostFormWithAntiforgeryAsync(
            "/SignIn?returnUrl=%2FOperations%2FQueue",
            "/SignIn",
            new Dictionary<string, string?>
            {
                ["Username"] = "hosted.admin",
                ["Password"] = "HostedAdmin123!",
                ["ReturnUrl"] = "/Operations/Queue"
            });

        Assert.Equal(HttpStatusCode.Redirect, signInResponse.StatusCode);
        Assert.Equal("/Operations/Queue", signInResponse.Headers.Location?.OriginalString);

        var queueResponse = await client.GetAsync($"/Operations/Queue?item={reviewItemId}");
        var queueHtml = await queueResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        Assert.Contains("operations-surface-grid", queueHtml, StringComparison.Ordinal);
        Assert.Contains(guaranteeNumber, queueHtml, StringComparison.Ordinal);
        Assert.Contains("name=\"correctionNote\"", queueHtml, StringComparison.Ordinal);
        Assert.Contains(reviewItemId.ToString(), queueHtml, StringComparison.Ordinal);

        var reopenResponse = await client.PostFormWithAntiforgeryAsync(
            $"/Operations/Queue?item={reviewItemId}",
            "/Operations/Queue?handler=ReopenApplied",
            new Dictionary<string, string?>
            {
                ["actorId"] = operationsActorId.ToString(),
                ["reviewItemId"] = reviewItemId.ToString(),
                ["correctionNote"] = "Mapped to the wrong request in operations.",
                ["item"] = reviewItemId.ToString(),
                ["page"] = "1"
            });

        Assert.Equal(HttpStatusCode.Redirect, reopenResponse.StatusCode);
        Assert.StartsWith("/Operations/Queue", reopenResponse.Headers.Location?.OriginalString, StringComparison.Ordinal);

        var reopenedState = await factory.QueryDbContextAsync(async dbContext =>
        {
            var request = await dbContext.GuaranteeRequests
                .SingleAsync(item => item.Id == requestId);
            var correspondence = await dbContext.GuaranteeCorrespondence
                .SingleAsync(item => item.Id == correspondenceId);
            var reviewItem = await dbContext.OperationsReviewItems
                .SingleAsync(item => item.Id == reviewItemId);
            var guarantee = await dbContext.Guarantees
                .Include(item => item.Events)
                .SingleAsync(item => item.GuaranteeNumber == guaranteeNumber);

            return new
            {
                RequestStatus = request.Status,
                RequestCompletedAtUtc = request.CompletedAtUtc,
                request.CompletionCorrespondenceId,
                CorrespondenceAppliedToGuaranteeAtUtc = correspondence.AppliedToGuaranteeAtUtc,
                ReviewItemStatus = reviewItem.Status,
                ReviewItemCompletedAtUtc = reviewItem.CompletedAtUtc,
                GuaranteeExpiryDate = guarantee.ExpiryDate,
                HasReopenEvent = guarantee.Events.Any(item =>
                    item.EventType == GuaranteeEventType.BankConfirmationReopened &&
                    item.GuaranteeRequestId == requestId &&
                    item.GuaranteeCorrespondenceId == correspondenceId)
            };
        });

        Assert.Equal(GuaranteeRequestStatus.AwaitingBankResponse, reopenedState.RequestStatus);
        Assert.Null(reopenedState.RequestCompletedAtUtc);
        Assert.Null(reopenedState.CompletionCorrespondenceId);
        Assert.Null(reopenedState.CorrespondenceAppliedToGuaranteeAtUtc);
        Assert.Equal(OperationsReviewItemStatus.Pending, reopenedState.ReviewItemStatus);
        Assert.Null(reopenedState.ReviewItemCompletedAtUtc);
        Assert.Equal(originalExpiryDate, reopenedState.GuaranteeExpiryDate);
        Assert.True(reopenedState.HasReopenEvent);
    }

    [Fact]
    public async Task Dispatch_workspace_loads_ready_requests_for_signed_in_actor()
    {
        await using var factory = new HostedAppFactory();
        const string guaranteeNumber = "BG-2026-DIS-1001";

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
        Assert.Contains("dispatch-surface-grid", dispatchHtml, StringComparison.Ordinal);
        Assert.Contains(guaranteeNumber, dispatchHtml, StringComparison.Ordinal);
        Assert.Contains("Dispatch Requester", dispatchHtml, StringComparison.Ordinal);
    }

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
        Assert.Equal("/Administration/Workflow", updateResponse.Headers.Location?.OriginalString);

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
            var approver = await SeedLocalUserAsync(
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

    private static async Task<User> SeedLocalUserAsync(
        BgDbContext dbContext,
        IServiceProvider serviceProvider,
        string username,
        string displayName,
        string password,
        params (string RoleName, string[] PermissionKeys)[] roles)
    {
        var roleEntities = new List<Role>();

        foreach (var (roleName, permissionKeys) in roles)
        {
            roleEntities.Add(await EnsureRoleAsync(dbContext, roleName, permissionKeys));
        }

        var user = new User(
            username,
            displayName,
            $"{username}@bg.local",
            externalId: null,
            UserSourceType.Local,
            isActive: true,
            createdAtUtc: DateTimeOffset.UtcNow);
        var passwordHasher = serviceProvider.GetRequiredService<ILocalPasswordHasher>();
        user.SetLocalPassword(passwordHasher.HashPassword(password), DateTimeOffset.UtcNow);
        user.AssignRoles(roleEntities);

        await dbContext.Users.AddAsync(user);
        return user;
    }

    private static async Task<Role> EnsureRoleAsync(BgDbContext dbContext, string name, IReadOnlyCollection<string> permissionKeys)
    {
        var normalizedRoleName = Role.NormalizeNameKey(name);
        var trackedRole = dbContext.Roles.Local.SingleOrDefault(role => role.NormalizedName == normalizedRoleName);
        if (trackedRole is not null)
        {
            return trackedRole;
        }

        var existingRole = await dbContext.Roles
            .Include(role => role.RolePermissions)
            .ThenInclude(rolePermission => rolePermission.Permission)
            .SingleOrDefaultAsync(role => role.NormalizedName == normalizedRoleName);

        if (existingRole is not null)
        {
            return existingRole;
        }

        var permissions = await dbContext.Permissions
            .Where(permission => permissionKeys.Contains(permission.Key))
            .ToArrayAsync();

        var role = new Role(name, $"{name} for hosted tests");
        role.AssignPermissions(permissions);

        await dbContext.Roles.AddAsync(role);
        return role;
    }

    private static void AttachRequestSourceDocument(
        Guarantee guarantee,
        GuaranteeRequest request,
        string fileName,
        string documentFormKey,
        string bankName)
    {
        var document = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.GuaranteeInstrument,
            fileName,
            $"/hosted/{fileName}",
            1,
            DateTimeOffset.UtcNow.AddDays(-11),
            intakeScenarioKey: IntakeScenarioKeys.NewGuarantee,
            extractionMethod: "OCR",
            verifiedDataJson: $$"""{"documentFormKey":"{{documentFormKey}}","bankName":"{{bankName}}"}""");

        guarantee.AttachDocumentToRequest(request.Id, document.Id, DateTimeOffset.UtcNow.AddDays(-11));
    }

    private static void ApproveAndDispatchRequest(
        GuaranteeRequest request,
        Guarantee guarantee,
        Guid workflowDefinitionId,
        Guid approverRoleId,
        Guid approverUserId,
        string outgoingReference,
        DateOnly outgoingLetterDate)
    {
        var process = new RequestApprovalProcess(request.Id, workflowDefinitionId, DateTimeOffset.UtcNow.AddDays(-8));
        process.AddStage(
            approverRoleId,
            "WorkflowStage_GuaranteesSupervisor_Title",
            "WorkflowStage_GuaranteesSupervisor_Summary",
            titleText: null,
            summaryText: null,
            requiresLetterSignature: true);
        process.Start();
        request.SubmitForApproval(process);
        process.ApproveCurrentStage(approverUserId, DateTimeOffset.UtcNow.AddDays(-7), note: "Approved");
        request.MarkApprovedForDispatch();

        guarantee.RecordOutgoingDispatch(
            request.Id,
            outgoingReference,
            outgoingLetterDate,
            GuaranteeDispatchChannel.Courier,
            "PKG-HOSTED-OPS",
            "Sent to bank",
            DateTimeOffset.UtcNow.AddDays(-6));
    }
}
