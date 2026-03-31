using System.Globalization;
using System.Text.Json;
using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Models.Approvals;
using BG.Application.Models.Dispatch;
using BG.Application.Models.Requests;
using BG.Application.Operations;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Operations;
using BG.Domain.Workflow;
using BG.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BG.Infrastructure.Persistence;

internal sealed class OperationalSeedService
{
    private const string OptionsSectionName = "OperationalSeed";
    private const string SharedExtractionRoute = "IntakeExtractionRoute_ManualReview";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] InsecureSharedPasswords =
    [
        "BG-Seed-2026!",
        "SeedUsers123!"
    ];

    private static readonly SeedRoleDefinition[] SeedRoles =
    [
        new("Intake Operators", "Seed role for intake verification users.", ["dashboard.view", "intake.view", "intake.scan", "intake.verify", "intake.finalize"]),
        new("Operations Reviewers", "Seed role for operations queue users.", ["dashboard.view", "operations.queue.view", "operations.queue.manage"]),
        new("Request Owners", "Seed role for request owner users.", ["dashboard.view", "requests.view", "requests.create"]),
        new("Dispatch Officers", "Seed role for dispatch operators.", ["dashboard.view", "dispatch.view", "dispatch.print", "dispatch.record", "dispatch.email"]),
        new("Guarantees Supervisors", "Seed approval role for guarantees supervisors.", ["dashboard.view", "approvals.queue.view", "approvals.sign"]),
        new("Department Managers", "Seed approval role for department managers.", ["dashboard.view", "approvals.queue.view", "approvals.sign"]),
        new("Program Directors", "Seed approval role for program directors.", ["dashboard.view", "approvals.queue.view", "approvals.sign"]),
        new("Deputy Financial Affairs Directors", "Seed approval role for deputy financial affairs directors.", ["dashboard.view", "approvals.queue.view", "approvals.sign"]),
        new("Contracts Signer 1", "Seed approval role for first contracts signer.", ["dashboard.view", "approvals.queue.view", "approvals.sign"]),
        new("Contracts Signer 2", "Seed approval role for second contracts signer.", ["dashboard.view", "approvals.queue.view", "approvals.sign"]),
        new("Contracts Signer 3", "Seed approval role for third contracts signer.", ["dashboard.view", "approvals.queue.view", "approvals.sign"]),
        new("Procurement Signer 1", "Seed approval role for first procurement signer.", ["dashboard.view", "approvals.queue.view", "approvals.sign"]),
        new("Procurement Signer 2", "Seed approval role for second procurement signer.", ["dashboard.view", "approvals.queue.view", "approvals.sign"]),
        new("Procurement Signer 3", "Seed approval role for third procurement signer.", ["dashboard.view", "approvals.queue.view", "approvals.sign"]),
        new("Executive Vice Presidents", "Seed approval role for executive vice presidents.", ["dashboard.view", "approvals.queue.view", "approvals.sign"]),
        new("System Administrator", "Full system administrator with all permissions.", ["dashboard.view", "intake.view", "intake.scan", "intake.verify", "intake.finalize", "operations.queue.view", "operations.queue.manage", "requests.view", "requests.create", "approvals.queue.view", "approvals.sign", "workflow.view", "workflow.manage", "dispatch.view", "dispatch.print", "dispatch.record", "dispatch.email", "users.view", "users.manage", "delegations.view", "delegations.manage", "roles.view", "roles.manage", "guarantees.view", "guarantees.manage"])
    ];

    private static readonly SeedUserDefinition[] SeedUsers =
    [
        new("intake.operator", "Intake Operator", "intake.operator@bg.local", ["Intake Operators"]),
        new("operations.reviewer", "Operations Reviewer", "operations.reviewer@bg.local", ["Operations Reviewers"]),
        new("request.owner1", "Request Owner One", "request.owner1@bg.local", ["Request Owners"]),
        new("request.owner2", "Request Owner Two", "request.owner2@bg.local", ["Request Owners"]),
        new("dispatch.officer", "Dispatch Officer", "dispatch.officer@bg.local", ["Dispatch Officers"]),
        new("guarantees.supervisor", "Guarantees Supervisor", "guarantees.supervisor@bg.local", ["Guarantees Supervisors"]),
        new("department.manager", "Department Manager", "department.manager@bg.local", ["Department Managers"]),
        new("program.director", "Program Director", "program.director@bg.local", ["Program Directors"]),
        new("deputy.financial", "Deputy Financial Affairs Director", "deputy.financial@bg.local", ["Deputy Financial Affairs Directors"]),
        new("contracts.signer1", "Contracts Signer 1", "contracts.signer1@bg.local", ["Contracts Signer 1"]),
        new("contracts.signer2", "Contracts Signer 2", "contracts.signer2@bg.local", ["Contracts Signer 2"]),
        new("contracts.signer3", "Contracts Signer 3", "contracts.signer3@bg.local", ["Contracts Signer 3"]),
        new("procurement.signer1", "Procurement Signer 1", "procurement.signer1@bg.local", ["Procurement Signer 1"]),
        new("procurement.signer2", "Procurement Signer 2", "procurement.signer2@bg.local", ["Procurement Signer 2"]),
        new("procurement.signer3", "Procurement Signer 3", "procurement.signer3@bg.local", ["Procurement Signer 3"]),
        new("executive.vp", "Executive Vice President", "executive.vp@bg.local", ["Executive Vice Presidents"]),
        new("administrator", "System Administrator", "admin@bg.local", ["System Administrator"])
    ];

    private static readonly IReadOnlyDictionary<string, string> WorkflowStageRoleNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["WorkflowStage_GuaranteesSupervisor_Title"] = "Guarantees Supervisors",
            ["WorkflowStage_DepartmentManager_Title"] = "Department Managers",
            ["WorkflowStage_ProgramDirector_Title"] = "Program Directors",
            ["WorkflowStage_DeputyFinancialAffairsDirector_Title"] = "Deputy Financial Affairs Directors",
            ["WorkflowStage_ContractsSigner1_Title"] = "Contracts Signer 1",
            ["WorkflowStage_ContractsSigner2_Title"] = "Contracts Signer 2",
            ["WorkflowStage_ContractsSigner3_Title"] = "Contracts Signer 3",
            ["WorkflowStage_ProcurementSigner1_Title"] = "Procurement Signer 1",
            ["WorkflowStage_ProcurementSigner2_Title"] = "Procurement Signer 2",
            ["WorkflowStage_ProcurementSigner3_Title"] = "Procurement Signer 3",
            ["WorkflowStage_ExecutiveVicePresident_Title"] = "Executive Vice Presidents"
        };

    private readonly BgDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILocalPasswordHasher _passwordHasher;
    private readonly IRequestWorkspaceService _requestWorkspaceService;
    private readonly IApprovalQueueService _approvalQueueService;
    private readonly IDispatchWorkspaceService _dispatchWorkspaceService;
    private readonly IOperationsReviewQueueService _operationsReviewQueueService;

    public OperationalSeedService(
        BgDbContext dbContext,
        IConfiguration configuration,
        ILocalPasswordHasher passwordHasher,
        IRequestWorkspaceService requestWorkspaceService,
        IApprovalQueueService approvalQueueService,
        IDispatchWorkspaceService dispatchWorkspaceService,
        IOperationsReviewQueueService operationsReviewQueueService)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _passwordHasher = passwordHasher;
        _requestWorkspaceService = requestWorkspaceService;
        _approvalQueueService = approvalQueueService;
        _dispatchWorkspaceService = dispatchWorkspaceService;
        _operationsReviewQueueService = operationsReviewQueueService;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var options = OperationalSeedOptions.FromConfiguration(_configuration.GetSection(OptionsSectionName));
        if (!options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.SharedPassword))
        {
            throw new InvalidOperationException(
                "OperationalSeed:SharedPassword must be configured explicitly when OperationalSeed is enabled.");
        }

        var sharedPassword = options.SharedPassword.Trim();
        if (InsecureSharedPasswords.Contains(sharedPassword, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                "OperationalSeed:SharedPassword cannot use the retired repository seed passwords. Configure a local secret instead.");
        }

        var permissions = await _dbContext.Permissions
            .OrderBy(permission => permission.Key)
            .ToDictionaryAsync(permission => permission.Key, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var rolesByName = await EnsureRolesAsync(permissions, cancellationToken);
        var usersByUsername = await EnsureUsersAsync(rolesByName, sharedPassword, cancellationToken);
        await EnsureWorkflowAssignmentsAsync(rolesByName, cancellationToken);
        await EnsureScenarioPackAsync(usersByUsername, cancellationToken);
    }

    private async Task<Dictionary<string, Role>> EnsureRolesAsync(
        IReadOnlyDictionary<string, Permission> permissions,
        CancellationToken cancellationToken)
    {
        var existingRoles = await _dbContext.Roles
            .Include(role => role.RolePermissions)
            .ToDictionaryAsync(role => role.NormalizedName, cancellationToken);

        foreach (var definition in SeedRoles)
        {
            if (!existingRoles.TryGetValue(Role.NormalizeNameKey(definition.Name), out var role))
            {
                role = new Role(definition.Name, definition.Description);
                await _dbContext.Roles.AddAsync(role, cancellationToken);
                existingRoles[role.NormalizedName] = role;
            }

            role.AssignPermissions(definition.PermissionKeys.Select(permissionKey => permissions[permissionKey]));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return existingRoles.Values.ToDictionary(role => role.Name, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<string, User>> EnsureUsersAsync(
        IReadOnlyDictionary<string, Role> rolesByName,
        string sharedPassword,
        CancellationToken cancellationToken)
    {
        var existingUsers = await _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .ToDictionaryAsync(user => user.NormalizedUsername, cancellationToken);

        foreach (var definition in SeedUsers)
        {
            if (!existingUsers.TryGetValue(User.NormalizeUsernameKey(definition.Username), out var user))
            {
                user = new User(
                    definition.Username,
                    definition.DisplayName,
                    definition.Email,
                    externalId: null,
                    UserSourceType.Local,
                    isActive: true,
                    createdAtUtc: DateTimeOffset.UtcNow);
                await _dbContext.Users.AddAsync(user, cancellationToken);
                existingUsers[user.NormalizedUsername] = user;
            }

            user.SetLocalPassword(_passwordHasher.HashPassword(sharedPassword), DateTimeOffset.UtcNow);
            user.AssignRoles(definition.RoleNames.Select(roleName => rolesByName[roleName]));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return existingUsers.Values.ToDictionary(user => user.Username, StringComparer.OrdinalIgnoreCase);
    }

    private async Task EnsureWorkflowAssignmentsAsync(
        IReadOnlyDictionary<string, Role> rolesByName,
        CancellationToken cancellationToken)
    {
        var definitions = await _dbContext.RequestWorkflowDefinitions
            .Include(definition => definition.Stages)
            .ToListAsync(cancellationToken);

        var hasChanges = false;

        foreach (var definition in definitions)
        {
            foreach (var stage in definition.Stages.OrderBy(stage => stage.Sequence))
            {
                if (stage.RoleId.HasValue ||
                    string.IsNullOrWhiteSpace(stage.TitleResourceKey) ||
                    !WorkflowStageRoleNames.TryGetValue(stage.TitleResourceKey, out var roleName))
                {
                    continue;
                }

                definition.UpdateStage(
                    stage.Id,
                    rolesByName[roleName].Id,
                    stage.CustomTitle,
                    stage.CustomSummary,
                    DateTimeOffset.UtcNow,
                    stage.RequiresLetterSignature,
                    stage.DelegationPolicy);
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureScenarioPackAsync(
        IReadOnlyDictionary<string, User> usersByUsername,
        CancellationToken cancellationToken)
    {
        ResetTrackingState();

        var intakeUser = usersByUsername["intake.operator"];
        var ownerOne = usersByUsername["request.owner1"];
        var ownerTwo = usersByUsername["request.owner2"];
        var dispatchUser = usersByUsername["dispatch.officer"];
        var operationsUser = usersByUsername["operations.reviewer"];
        var approvalUsersByRoleId = await LoadApprovalUsersByRoleIdAsync(cancellationToken);

        await EnsureDraftExtendScenarioAsync(intakeUser, ownerOne, cancellationToken);
        await EnsureReturnedReduceScenarioAsync(intakeUser, ownerOne, approvalUsersByRoleId, cancellationToken);
        await EnsureInApprovalExtendScenarioAsync(intakeUser, ownerTwo, approvalUsersByRoleId, cancellationToken);
        await EnsureReadyForDispatchReleaseScenarioAsync(intakeUser, ownerOne, approvalUsersByRoleId, cancellationToken);
        await EnsurePendingDeliveryScenarioAsync(intakeUser, ownerTwo, dispatchUser, approvalUsersByRoleId, cancellationToken);
        await EnsurePendingOperationsExtensionScenarioAsync(intakeUser, ownerOne, dispatchUser, approvalUsersByRoleId, cancellationToken);
        await EnsureRoutedOperationsStatusScenarioAsync(intakeUser, ownerTwo, dispatchUser, approvalUsersByRoleId, cancellationToken);
        await EnsureCompletedReductionScenarioAsync(intakeUser, ownerOne, dispatchUser, operationsUser, approvalUsersByRoleId, cancellationToken);
    }

    private async Task<Dictionary<Guid, User>> LoadApprovalUsersByRoleIdAsync(CancellationToken cancellationToken)
    {
        var approvalRoleNames = WorkflowStageRoleNames.Values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return await _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .Where(user => user.UserRoles.Any(userRole => approvalRoleNames.Contains(userRole.Role.Name)))
            .ToDictionaryAsync(
                user => user.UserRoles.Single(userRole => approvalRoleNames.Contains(userRole.Role.Name)).RoleId,
                cancellationToken);
    }

    private async Task EnsureDraftExtendScenarioAsync(
        User intakeUser,
        User owner,
        CancellationToken cancellationToken)
    {
        var guarantee = await EnsureGuaranteeAsync(
            "BG-SEED-CT-0001",
            "Saudi National Bank",
            GuaranteeCategory.Contract,
            325000m,
            intakeUser,
            cancellationToken);
        var request = await EnsureRequestAsync(
            guarantee.GuaranteeNumber,
            owner,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: guarantee.ExpiryDate.AddDays(90),
            notes: "Seed draft extension request.",
            cancellationToken);

        _ = request;
    }

    private async Task EnsureReturnedReduceScenarioAsync(
        User intakeUser,
        User owner,
        IReadOnlyDictionary<Guid, User> approvalUsersByRoleId,
        CancellationToken cancellationToken)
    {
        var guarantee = await EnsureGuaranteeAsync(
            "BG-SEED-PO-0002",
            "Al Rajhi Bank",
            GuaranteeCategory.PurchaseOrder,
            640000m,
            intakeUser,
            cancellationToken);
        var request = await EnsureRequestAsync(
            guarantee.GuaranteeNumber,
            owner,
            GuaranteeRequestType.Reduce,
            requestedAmount: 590000m,
            requestedExpiryDate: null,
            notes: "Seed request waiting after return.",
            cancellationToken);

        if (request.Status == GuaranteeRequestStatus.Draft)
        {
            await SubmitRequestAsync(owner.Id, request.Id, cancellationToken);
        }

        request = await LoadRequestGraphAsync(request.Id, cancellationToken);

        if (request.Status == GuaranteeRequestStatus.InApproval &&
            request.ApprovalProcess?.GetCurrentStage()?.RoleId is Guid stageRoleId &&
            approvalUsersByRoleId.TryGetValue(stageRoleId, out var approver))
        {
            await ReturnRequestAsync(approver.Id, request.Id, "Seed return for request-owner revision path.", cancellationToken);
        }
    }

    private async Task EnsureInApprovalExtendScenarioAsync(
        User intakeUser,
        User owner,
        IReadOnlyDictionary<Guid, User> approvalUsersByRoleId,
        CancellationToken cancellationToken)
    {
        var guarantee = await EnsureGuaranteeAsync(
            "BG-SEED-CT-0003",
            "Riyad Bank",
            GuaranteeCategory.Contract,
            910000m,
            intakeUser,
            cancellationToken);
        var request = await EnsureRequestAsync(
            guarantee.GuaranteeNumber,
            owner,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: guarantee.ExpiryDate.AddDays(120),
            notes: "Seed in-approval extension request.",
            cancellationToken);

        if (request.Status == GuaranteeRequestStatus.Draft)
        {
            await SubmitRequestAsync(owner.Id, request.Id, cancellationToken);
        }

        request = await LoadRequestGraphAsync(request.Id, cancellationToken);

        if (request.Status == GuaranteeRequestStatus.InApproval &&
            request.ApprovalProcess?.Stages.Count(stage => stage.Status == RequestApprovalStageStatus.Approved) == 0 &&
            request.ApprovalProcess.GetCurrentStage()?.RoleId is Guid stageRoleId &&
            approvalUsersByRoleId.TryGetValue(stageRoleId, out var approver))
        {
            await ApproveRequestAsync(approver.Id, request.Id, "Seed first-stage approval.", cancellationToken);
        }
    }

    private async Task EnsureReadyForDispatchReleaseScenarioAsync(
        User intakeUser,
        User owner,
        IReadOnlyDictionary<Guid, User> approvalUsersByRoleId,
        CancellationToken cancellationToken)
    {
        var guarantee = await EnsureGuaranteeAsync(
            "BG-SEED-CT-0004",
            "Saudi National Bank",
            GuaranteeCategory.Contract,
            1180000m,
            intakeUser,
            cancellationToken);
        var request = await EnsureRequestAsync(
            guarantee.GuaranteeNumber,
            owner,
            GuaranteeRequestType.Release,
            requestedAmount: null,
            requestedExpiryDate: null,
            notes: "Seed release request ready for dispatch.",
            cancellationToken);

        await EnsureApprovedForDispatchAsync(request.Id, owner.Id, approvalUsersByRoleId, cancellationToken);
    }

    private async Task EnsurePendingDeliveryScenarioAsync(
        User intakeUser,
        User owner,
        User dispatchUser,
        IReadOnlyDictionary<Guid, User> approvalUsersByRoleId,
        CancellationToken cancellationToken)
    {
        var guarantee = await EnsureGuaranteeAsync(
            "BG-SEED-PO-0005",
            "Arab National Bank",
            GuaranteeCategory.PurchaseOrder,
            455000m,
            intakeUser,
            cancellationToken);
        var request = await EnsureRequestAsync(
            guarantee.GuaranteeNumber,
            owner,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: guarantee.ExpiryDate.AddDays(75),
            notes: "Seed dispatch item pending delivery confirmation.",
            cancellationToken);

        await EnsureApprovedForDispatchAsync(request.Id, owner.Id, approvalUsersByRoleId, cancellationToken);
        request = await LoadRequestGraphAsync(request.Id, cancellationToken);

        if (request.Status == GuaranteeRequestStatus.ApprovedForDispatch)
        {
            var referenceNumber = BuildOutgoingReference(guarantee.GuaranteeNumber, "PENDING-DELIVERY");
            var letterDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-2));

            await PrintDispatchAsync(dispatchUser.Id, request.Id, referenceNumber, letterDate, cancellationToken);
            await RecordDispatchAsync(
                dispatchUser.Id,
                request.Id,
                referenceNumber,
                letterDate,
                GuaranteeDispatchChannel.Courier,
                $"CR-{guarantee.GuaranteeNumber}",
                "Seed courier dispatch awaiting delivery confirmation.",
                cancellationToken);
        }
    }

    private async Task EnsurePendingOperationsExtensionScenarioAsync(
        User intakeUser,
        User owner,
        User dispatchUser,
        IReadOnlyDictionary<Guid, User> approvalUsersByRoleId,
        CancellationToken cancellationToken)
    {
        var guarantee = await EnsureGuaranteeAsync(
            "BG-SEED-CT-0006",
            "Bank Albilad",
            GuaranteeCategory.Contract,
            530000m,
            intakeUser,
            cancellationToken);
        var request = await EnsureRequestAsync(
            guarantee.GuaranteeNumber,
            owner,
            GuaranteeRequestType.Extend,
            requestedAmount: null,
            requestedExpiryDate: guarantee.ExpiryDate.AddDays(150),
            notes: "Seed operations item awaiting extension confirmation.",
            cancellationToken);

        await EnsureAwaitingBankResponseAsync(request.Id, owner.Id, dispatchUser.Id, approvalUsersByRoleId, cancellationToken);
        await EnsureOpenIncomingReviewItemAsync(
            guarantee.GuaranteeNumber,
            IntakeScenarioKeys.ExtensionConfirmation,
            OperationsReviewItemCategory.IncomingBankConfirmation,
            GuaranteeCorrespondenceKind.BankConfirmation,
            "EXT-88061",
            verifiedData: CreateVerifiedDataPayload(
                IntakeScenarioKeys.ExtensionConfirmation,
                guarantee.GuaranteeNumber,
                bankReference: "EXT-88061",
                officialLetterDate: DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1)),
                newExpiryDate: DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(365))),
            intakeUser,
            routeToLane: null,
            cancellationToken);
    }

    private async Task EnsureRoutedOperationsStatusScenarioAsync(
        User intakeUser,
        User owner,
        User dispatchUser,
        IReadOnlyDictionary<Guid, User> approvalUsersByRoleId,
        CancellationToken cancellationToken)
    {
        var guarantee = await EnsureGuaranteeAsync(
            "BG-SEED-PO-0007",
            "Banque Saudi Fransi",
            GuaranteeCategory.PurchaseOrder,
            275000m,
            intakeUser,
            cancellationToken);
        var request = await EnsureRequestAsync(
            guarantee.GuaranteeNumber,
            owner,
            GuaranteeRequestType.VerifyStatus,
            requestedAmount: null,
            requestedExpiryDate: null,
            notes: "Seed routed status verification item.",
            cancellationToken);

        await EnsureAwaitingBankResponseAsync(request.Id, owner.Id, dispatchUser.Id, approvalUsersByRoleId, cancellationToken);
        await EnsureOpenIncomingReviewItemAsync(
            guarantee.GuaranteeNumber,
            IntakeScenarioKeys.StatusVerification,
            OperationsReviewItemCategory.IncomingStatusReply,
            GuaranteeCorrespondenceKind.BankStatusReply,
            "STAT-44110",
            verifiedData: CreateVerifiedDataPayload(
                IntakeScenarioKeys.StatusVerification,
                guarantee.GuaranteeNumber,
                bankReference: "STAT-44110",
                officialLetterDate: DateOnly.FromDateTime(DateTime.UtcNow.Date),
                statusStatement: "Guarantee remains active and unchanged."),
            intakeUser,
            routeToLane: "OperationsReviewLane_StatusAssessment",
            cancellationToken);
    }

    private async Task EnsureCompletedReductionScenarioAsync(
        User intakeUser,
        User owner,
        User dispatchUser,
        User operationsUser,
        IReadOnlyDictionary<Guid, User> approvalUsersByRoleId,
        CancellationToken cancellationToken)
    {
        var guarantee = await EnsureGuaranteeAsync(
            "BG-SEED-CT-0008",
            "Saudi Investment Bank",
            GuaranteeCategory.Contract,
            760000m,
            intakeUser,
            cancellationToken);
        var request = await EnsureRequestAsync(
            guarantee.GuaranteeNumber,
            owner,
            GuaranteeRequestType.Reduce,
            requestedAmount: 705000m,
            requestedExpiryDate: null,
            notes: "Seed completed reduction after bank confirmation.",
            cancellationToken);

        await EnsureAwaitingBankResponseAsync(request.Id, owner.Id, dispatchUser.Id, approvalUsersByRoleId, cancellationToken);
        request = await LoadRequestGraphAsync(request.Id, cancellationToken);

        if (request.Status == GuaranteeRequestStatus.Completed)
        {
            return;
        }

        var reviewItem = await EnsureOpenIncomingReviewItemAsync(
            guarantee.GuaranteeNumber,
            IntakeScenarioKeys.ReductionConfirmation,
            OperationsReviewItemCategory.IncomingBankConfirmation,
            GuaranteeCorrespondenceKind.BankConfirmation,
            "RED-66251",
            verifiedData: CreateVerifiedDataPayload(
                IntakeScenarioKeys.ReductionConfirmation,
                guarantee.GuaranteeNumber,
                bankReference: "RED-66251",
                officialLetterDate: DateOnly.FromDateTime(DateTime.UtcNow.Date),
                amount: 705000m),
            intakeUser,
            routeToLane: "OperationsReviewLane_BankConfirmationReview",
            cancellationToken);

        if (reviewItem.Status != OperationsReviewItemStatus.Completed)
        {
            await ApplyBankResponseAsync(
                operationsUser.Id,
                reviewItem.Id,
                request.Id,
                confirmedAmount: 705000m,
                note: "Seed bank confirmation applied.",
                cancellationToken);
        }
    }

    private async Task<Guarantee> EnsureGuaranteeAsync(
        string guaranteeNumber,
        string bankName,
        GuaranteeCategory category,
        decimal currentAmount,
        User intakeUser,
        CancellationToken cancellationToken)
    {
        var existing = await LoadGuaranteeGraphAsync(guaranteeNumber, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var guarantee = Guarantee.RegisterNew(
            guaranteeNumber,
            bankName,
            "King Faisal Specialist Hospital & Research Centre",
            $"Seed Principal for {guaranteeNumber}",
            category,
            currentAmount,
            "SAR",
            today.AddMonths(-2),
            today.AddMonths(6),
            DateTimeOffset.UtcNow.AddDays(-10),
            externalReference: $"EXT-{guaranteeNumber}");

        guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.GuaranteeInstrument,
            $"{guaranteeNumber}-instrument.pdf",
            $"/seed/{guaranteeNumber}/instrument.pdf",
            3,
            DateTimeOffset.UtcNow.AddDays(-9),
            intakeUser.Id,
            intakeUser.DisplayName,
            GuaranteeDocumentCaptureChannel.ScanStation,
            "KFSHRC Scan Station",
            $"{guaranteeNumber}-SCAN",
            IntakeScenarioKeys.NewGuarantee,
            SharedExtractionRoute,
            CreateVerifiedDataPayload(
                IntakeScenarioKeys.NewGuarantee,
                guaranteeNumber,
                bankName: bankName,
                beneficiaryName: "King Faisal Specialist Hospital & Research Centre",
                principalName: $"Seed Principal for {guaranteeNumber}",
                guaranteeCategory: category.ToString(),
                amount: currentAmount,
                currencyCode: "SAR",
                issueDate: today.AddMonths(-2),
                expiryDate: today.AddMonths(6)));

        guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.SupportingDocument,
            $"{guaranteeNumber}-supporting.pdf",
            $"/seed/{guaranteeNumber}/supporting.pdf",
            1,
            DateTimeOffset.UtcNow.AddDays(-8),
            intakeUser.Id,
            intakeUser.DisplayName,
            GuaranteeDocumentCaptureChannel.OracleImport,
            "Oracle Contracts",
            $"{guaranteeNumber}-ORACLE",
            IntakeScenarioKeys.SupportingAttachment,
            "IntakeExtractionRoute_SourceImport",
            CreateVerifiedDataPayload(
                IntakeScenarioKeys.SupportingAttachment,
                guaranteeNumber,
                attachmentNote: "Seed supporting attachment imported from Oracle."));

        await _dbContext.Guarantees.AddAsync(guarantee, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await LoadGuaranteeGraphAsync(guaranteeNumber, cancellationToken)
               ?? throw new InvalidOperationException($"Failed to load seeded guarantee {guaranteeNumber}.");
    }

    private async Task<GuaranteeRequest> EnsureRequestAsync(
        string guaranteeNumber,
        User owner,
        GuaranteeRequestType requestType,
        decimal? requestedAmount,
        DateOnly? requestedExpiryDate,
        string notes,
        CancellationToken cancellationToken)
    {
        var guarantee = await LoadGuaranteeGraphAsync(guaranteeNumber, cancellationToken)
                        ?? throw new InvalidOperationException($"Guarantee {guaranteeNumber} was not found for seed request creation.");

        var existing = guarantee.Requests
            .OrderByDescending(request => request.CreatedAtUtc)
            .FirstOrDefault(request => request.RequestType == requestType && request.RequestedByUserId == owner.Id);
        if (existing is not null)
        {
            return existing;
        }

        var result = await _requestWorkspaceService.CreateRequestAsync(
            new CreateGuaranteeRequestCommand(
                owner.Id,
                guaranteeNumber,
                requestType,
                requestedAmount?.ToString("0.##", CultureInfo.InvariantCulture),
                requestedExpiryDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                notes),
            cancellationToken);

        return await GetRequestOrThrowAsync(result, cancellationToken);
    }

    private async Task EnsureApprovedForDispatchAsync(
        Guid requestId,
        Guid ownerUserId,
        IReadOnlyDictionary<Guid, User> approvalUsersByRoleId,
        CancellationToken cancellationToken)
    {
        var request = await LoadRequestGraphAsync(requestId, cancellationToken);

        if (request.Status == GuaranteeRequestStatus.Draft || request.Status == GuaranteeRequestStatus.Returned)
        {
            await SubmitRequestAsync(ownerUserId, requestId, cancellationToken);
            request = await LoadRequestGraphAsync(requestId, cancellationToken);
        }

        while (request.Status == GuaranteeRequestStatus.InApproval)
        {
            var currentStageRoleId = request.ApprovalProcess?.GetCurrentStage()?.RoleId;
            if (!currentStageRoleId.HasValue || !approvalUsersByRoleId.TryGetValue(currentStageRoleId.Value, out var approver))
            {
                throw new InvalidOperationException($"No seeded approver exists for active stage on request {request.Id}.");
            }

            await ApproveRequestAsync(approver.Id, request.Id, "Seed approval progression.", cancellationToken);
            request = await LoadRequestGraphAsync(requestId, cancellationToken);
        }
    }

    private async Task EnsureAwaitingBankResponseAsync(
        Guid requestId,
        Guid ownerUserId,
        Guid dispatcherUserId,
        IReadOnlyDictionary<Guid, User> approvalUsersByRoleId,
        CancellationToken cancellationToken)
    {
        await EnsureApprovedForDispatchAsync(requestId, ownerUserId, approvalUsersByRoleId, cancellationToken);

        var request = await LoadRequestGraphAsync(requestId, cancellationToken);
        if (request.Status != GuaranteeRequestStatus.ApprovedForDispatch)
        {
            return;
        }

        var referenceNumber = BuildOutgoingReference(request.Guarantee.GuaranteeNumber, "BANK");
        var letterDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-3));

        await PrintDispatchAsync(dispatcherUserId, request.Id, referenceNumber, letterDate, cancellationToken);
        await RecordDispatchAsync(
            dispatcherUserId,
            request.Id,
            referenceNumber,
            letterDate,
            GuaranteeDispatchChannel.InternalMail,
            $"RM-{request.Guarantee.GuaranteeNumber}",
            "Seed dispatch recorded for bank response tracking.",
            cancellationToken);

        request = await LoadRequestGraphAsync(requestId, cancellationToken);
        var correspondenceId = request.Correspondence
            .Where(correspondence => correspondence.Direction == GuaranteeCorrespondenceDirection.Outgoing &&
                                     correspondence.Kind == GuaranteeCorrespondenceKind.RequestLetter)
            .Select(correspondence => correspondence.Id)
            .Single();

        await ConfirmDeliveryAsync(
            dispatcherUserId,
            request.Id,
            correspondenceId,
            $"DEL-{request.Guarantee.GuaranteeNumber}",
            "Seed delivery confirmation recorded.",
            cancellationToken);
    }

    private async Task<OperationsReviewItem> EnsureOpenIncomingReviewItemAsync(
        string guaranteeNumber,
        string scenarioKey,
        OperationsReviewItemCategory category,
        GuaranteeCorrespondenceKind correspondenceKind,
        string bankReference,
        string verifiedData,
        User intakeUser,
        string? routeToLane,
        CancellationToken cancellationToken)
    {
        var guarantee = await LoadGuaranteeGraphAsync(guaranteeNumber, cancellationToken)
                        ?? throw new InvalidOperationException($"Guarantee {guaranteeNumber} was not found for operations review seeding.");

        var existingQuery = _dbContext.OperationsReviewItems
            .Where(item => item.GuaranteeNumber == guaranteeNumber &&
                           item.ScenarioKey == scenarioKey &&
                           item.Status != OperationsReviewItemStatus.Completed);

        var existing = RepositoryPaging.RequiresClientSideTemporalOrdering(_dbContext)
            ? (await existingQuery.ToListAsync(cancellationToken))
                .OrderByDescending(item => item.CreatedAtUtc)
                .FirstOrDefault()
            : await existingQuery
                .OrderByDescending(item => item.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var capturedAtUtc = DateTimeOffset.UtcNow.AddHours(-2);
        var document = guarantee.RegisterScannedDocument(
            GuaranteeDocumentType.BankResponse,
            $"{guaranteeNumber}-{scenarioKey}.pdf",
            $"/seed/{guaranteeNumber}/{scenarioKey}.pdf",
            1,
            capturedAtUtc,
            intakeUser.Id,
            intakeUser.DisplayName,
            GuaranteeDocumentCaptureChannel.ManualUpload,
            sourceSystemName: null,
            sourceReference: null,
            intakeScenarioKey: scenarioKey,
            extractionMethod: SharedExtractionRoute,
            verifiedDataJson: verifiedData,
            notes: "Seed incoming bank response.");

        var correspondence = guarantee.RegisterCorrespondence(
            requestId: null,
            GuaranteeCorrespondenceDirection.Incoming,
            correspondenceKind,
            bankReference,
            DateOnly.FromDateTime(DateTime.UtcNow.Date),
            document.Id,
            "Seed incoming bank response correspondence.",
            capturedAtUtc,
            intakeUser.Id,
            intakeUser.DisplayName);

        var reviewItem = new OperationsReviewItem(
            guarantee.Id,
            guarantee.GuaranteeNumber,
            document.Id,
            correspondence.Id,
            scenarioKey,
            category,
            capturedAtUtc);

        if (!string.IsNullOrWhiteSpace(routeToLane))
        {
            reviewItem.RouteTo(routeToLane, DateTimeOffset.UtcNow.AddHours(-1));
        }

        await _dbContext.OperationsReviewItems.AddAsync(reviewItem, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return reviewItem;
    }

    private async Task SubmitRequestAsync(Guid ownerUserId, Guid requestId, CancellationToken cancellationToken)
    {
        ResetTrackingState();
        var result = await _requestWorkspaceService.SubmitRequestForApprovalAsync(ownerUserId, requestId, cancellationToken);
        EnsureSuccess(result, $"Failed to submit seeded request {requestId} for approval.");
    }

    private async Task ApproveRequestAsync(Guid approverUserId, Guid requestId, string note, CancellationToken cancellationToken)
    {
        ResetTrackingState();
        var result = await _approvalQueueService.ApproveAsync(new ApprovalDecisionCommand(approverUserId, requestId, note), cancellationToken);
        EnsureSuccess(result, $"Failed to approve seeded request {requestId}.");
    }

    private async Task ReturnRequestAsync(Guid approverUserId, Guid requestId, string note, CancellationToken cancellationToken)
    {
        ResetTrackingState();
        var result = await _approvalQueueService.ReturnAsync(new ApprovalDecisionCommand(approverUserId, requestId, note), cancellationToken);
        EnsureSuccess(result, $"Failed to return seeded request {requestId}.");
    }

    private async Task PrintDispatchAsync(
        Guid dispatcherUserId,
        Guid requestId,
        string referenceNumber,
        DateOnly letterDate,
        CancellationToken cancellationToken)
    {
        ResetTrackingState();
        var result = await _dispatchWorkspaceService.PrintDispatchLetterAsync(
            new PrintDispatchLetterCommand(
                dispatcherUserId,
                requestId,
                referenceNumber,
                letterDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                GuaranteeOutgoingLetterPrintMode.WorkstationPrinter),
            cancellationToken);
        EnsureSuccess(result, $"Failed to print seeded dispatch letter for request {requestId}.");
    }

    private async Task RecordDispatchAsync(
        Guid dispatcherUserId,
        Guid requestId,
        string referenceNumber,
        DateOnly letterDate,
        GuaranteeDispatchChannel dispatchChannel,
        string dispatchReference,
        string note,
        CancellationToken cancellationToken)
    {
        ResetTrackingState();
        var result = await _dispatchWorkspaceService.RecordDispatchAsync(
            new RecordDispatchCommand(
                dispatcherUserId,
                requestId,
                referenceNumber,
                letterDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                dispatchChannel,
                dispatchReference,
                note),
            cancellationToken);
        EnsureSuccess(result, $"Failed to record seeded dispatch for request {requestId}.");
    }

    private async Task ConfirmDeliveryAsync(
        Guid dispatcherUserId,
        Guid requestId,
        Guid correspondenceId,
        string deliveryReference,
        string deliveryNote,
        CancellationToken cancellationToken)
    {
        ResetTrackingState();
        var result = await _dispatchWorkspaceService.ConfirmDeliveryAsync(
            new ConfirmDispatchDeliveryCommand(
                dispatcherUserId,
                requestId,
                correspondenceId,
                deliveryReference,
                deliveryNote),
            cancellationToken);
        EnsureSuccess(result, $"Failed to confirm seeded dispatch delivery for request {requestId}.");
    }

    private async Task ApplyBankResponseAsync(
        Guid operationsActorUserId,
        Guid reviewItemId,
        Guid requestId,
        decimal confirmedAmount,
        string note,
        CancellationToken cancellationToken)
    {
        ResetTrackingState();
        var result = await _operationsReviewQueueService.ApplyBankResponseAsync(
            new ApplyBankResponseCommand(
                operationsActorUserId,
                reviewItemId,
                requestId,
                ConfirmedExpiryDate: null,
                ConfirmedAmount: confirmedAmount.ToString("0.##", CultureInfo.InvariantCulture),
                ReplacementGuaranteeNumber: null,
                Note: note),
            cancellationToken);
        EnsureSuccess(result, $"Failed to apply seeded bank response for review item {reviewItemId}.");
    }

    private async Task<Guarantee?> LoadGuaranteeGraphAsync(string guaranteeNumber, CancellationToken cancellationToken)
    {
        return await _dbContext.Guarantees
            .Include(guarantee => guarantee.Documents)
            .ThenInclude(document => document.RequestLinks)
            .Include(guarantee => guarantee.Correspondence)
            .Include(guarantee => guarantee.Events)
            .Include(guarantee => guarantee.Requests)
            .ThenInclude(request => request.Correspondence)
            .Include(guarantee => guarantee.Requests)
            .ThenInclude(request => request.RequestDocuments)
            .Include(guarantee => guarantee.Requests)
            .ThenInclude(request => request.ApprovalProcess)
            .ThenInclude(process => process!.Stages)
            .SingleOrDefaultAsync(guarantee => guarantee.GuaranteeNumber == guaranteeNumber, cancellationToken);
    }

    private async Task<GuaranteeRequest> LoadRequestGraphAsync(Guid requestId, CancellationToken cancellationToken)
    {
        return await _dbContext.GuaranteeRequests
            .Include(request => request.Guarantee)
            .ThenInclude(guarantee => guarantee.Correspondence)
            .Include(request => request.Correspondence)
            .Include(request => request.ApprovalProcess)
            .ThenInclude(process => process!.Stages)
            .SingleAsync(request => request.Id == requestId, cancellationToken);
    }

    private async Task<GuaranteeRequest> GetRequestOrThrowAsync(
        BG.Application.Common.OperationResult<BG.Application.Models.Requests.CreateGuaranteeRequestReceiptDto> result,
        CancellationToken cancellationToken)
    {
        EnsureSuccess(result, "Failed to create seeded request.");
        return await LoadRequestGraphAsync(result.Value!.RequestId, cancellationToken);
    }

    private void ResetTrackingState()
    {
        _dbContext.ChangeTracker.Clear();
    }

    private static void EnsureSuccess<T>(BG.Application.Common.OperationResult<T> result, string message)
    {
        if (!result.Succeeded || result.Value is null)
        {
            throw new InvalidOperationException($"{message} Error: {result.ErrorCode ?? "unknown"}");
        }
    }

    private static string BuildOutgoingReference(string guaranteeNumber, string suffix)
    {
        return $"{suffix}-{guaranteeNumber}";
    }

    private static string CreateVerifiedDataPayload(
        string scenarioKey,
        string guaranteeNumber,
        string? bankReference = null,
        DateOnly? officialLetterDate = null,
        DateOnly? newExpiryDate = null,
        decimal? amount = null,
        string? statusStatement = null,
        string? bankName = null,
        string? beneficiaryName = null,
        string? principalName = null,
        string? guaranteeCategory = null,
        string? currencyCode = null,
        DateOnly? issueDate = null,
        DateOnly? expiryDate = null,
        string? attachmentNote = null)
    {
        var payload = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["scenarioKey"] = scenarioKey,
            ["guaranteeNumber"] = guaranteeNumber,
            ["bankName"] = bankName,
            ["beneficiaryName"] = beneficiaryName,
            ["principalName"] = principalName,
            ["guaranteeCategory"] = guaranteeCategory,
            ["amount"] = amount?.ToString("0.##", CultureInfo.InvariantCulture),
            ["currencyCode"] = currencyCode,
            ["issueDate"] = issueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["expiryDate"] = expiryDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["officialLetterDate"] = officialLetterDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["newExpiryDate"] = newExpiryDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["bankReference"] = bankReference,
            ["statusStatement"] = statusStatement,
            ["attachmentNote"] = attachmentNote,
            ["captureChannel"] = GuaranteeDocumentCaptureChannel.ManualUpload.ToString(),
            ["sourceSystemName"] = null,
            ["sourceReference"] = null,
            ["extractionRoute"] = SharedExtractionRoute
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private sealed record OperationalSeedOptions(bool Enabled, string? SharedPassword)
    {
        public static OperationalSeedOptions FromConfiguration(IConfiguration section)
        {
            var enabled = bool.TryParse(section["Enabled"], out var parsedEnabled) && parsedEnabled;
            return new OperationalSeedOptions(
                enabled,
                section["SharedPassword"]);
        }
    }

    private sealed record SeedRoleDefinition(string Name, string Description, IReadOnlyList<string> PermissionKeys);

    private sealed record SeedUserDefinition(string Username, string DisplayName, string Email, IReadOnlyList<string> RoleNames);
}
