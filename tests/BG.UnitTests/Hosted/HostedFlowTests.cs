using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Security;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Workflow;
using BG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BG.UnitTests.Hosted;

public sealed partial class HostedFlowTests
{
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
