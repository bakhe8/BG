using System.Net;
using BG.Application.Intake;
using BG.Domain.Guarantees;
using BG.Domain.Operations;
using BG.Domain.Workflow;
using Microsoft.EntityFrameworkCore;

namespace BG.UnitTests.Hosted;

public sealed partial class HostedFlowTests
{
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
        Assert.True(
            queueHtml.Contains("operations-surface-grid", StringComparison.Ordinal) ||
            queueHtml.Contains("operations-surface-solo", StringComparison.Ordinal));
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
        Assert.True(
            queueHtml.Contains("operations-surface-grid", StringComparison.Ordinal) ||
            queueHtml.Contains("operations-surface-solo", StringComparison.Ordinal));
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
        Assert.True(
            queueHtml.Contains("operations-surface-grid", StringComparison.Ordinal) ||
            queueHtml.Contains("operations-surface-solo", StringComparison.Ordinal));
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
}
