using BG.Application.Security;
using BG.Application.Contracts.Services;
using BG.Domain.Identity;
using BG.Domain.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BG.Infrastructure.Persistence;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeInfrastructureAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BgDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<ILocalPasswordHasher>();

        if (string.Equals(dbContext.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal))
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }

        var existingKeys = await dbContext.Permissions
            .Select(permission => permission.Key)
            .ToListAsync(cancellationToken);

        var missingPermissions = PermissionCatalog.Definitions
            .Where(definition => !existingKeys.Contains(definition.Key, StringComparer.OrdinalIgnoreCase))
            .Select(definition => new Permission(definition.Key, definition.Area))
            .ToArray();

        if (missingPermissions.Length > 0)
        {
            await dbContext.Permissions.AddRangeAsync(missingPermissions, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var existingWorkflowKeys = await dbContext.RequestWorkflowDefinitions
            .Select(definition => definition.Key)
            .ToListAsync(cancellationToken);

        var missingDefinitions = BG.Application.Operations.RequestWorkflowTemplateCatalog.GetAll()
            .Where(template => !existingWorkflowKeys.Contains(template.Key, StringComparer.OrdinalIgnoreCase))
            .Select(template => CreateWorkflowDefinition(template))
            .ToArray();

        if (missingDefinitions.Length > 0)
        {
            await dbContext.RequestWorkflowDefinitions.AddRangeAsync(missingDefinitions, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var workflowDefinitions = await dbContext.RequestWorkflowDefinitions
            .Include(definition => definition.Stages)
            .ToListAsync(cancellationToken);

        var workflowStatusChanged = false;
        foreach (var definition in workflowDefinitions)
        {
            workflowStatusChanged |= definition.AlignOperationalStatus();
        }

        if (workflowStatusChanged)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await SeedBootstrapAdminAsync(dbContext, configuration, passwordHasher, cancellationToken);

        var operationalSeedService = scope.ServiceProvider.GetRequiredService<OperationalSeedService>();
        await operationalSeedService.SeedAsync(cancellationToken);
    }

    private static RequestWorkflowDefinition CreateWorkflowDefinition(BG.Application.Operations.RequestWorkflowTemplateDto template)
    {
        var definition = new RequestWorkflowDefinition(
            template.Key,
            template.RequestType,
            template.GuaranteeCategory,
            template.GuaranteeCategoryResourceKey,
            template.TitleResourceKey,
            template.SummaryResourceKey,
            DateTimeOffset.UtcNow,
            template.FinalSignatureDelegationPolicy,
            template.DelegationAmountThreshold);

        foreach (var stage in template.Stages.OrderBy(stage => stage.Level))
        {
            definition.AddStage(
                roleId: null,
                stage.TitleResourceKey,
                stage.SummaryResourceKey,
                customTitle: null,
                customSummary: null,
                stage.RequiresLetterSignature,
                DateTimeOffset.UtcNow,
                stage.DelegationPolicy);
        }

        return definition;
    }

    private static async Task SeedBootstrapAdminAsync(
        BgDbContext dbContext,
        IConfiguration configuration,
        ILocalPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        var bootstrapSection = configuration.GetSection("Identity:BootstrapAdmin");
        var username = bootstrapSection["Username"];
        var password = bootstrapSection["Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var permissions = await dbContext.Permissions
            .OrderBy(permission => permission.Key)
            .ToArrayAsync(cancellationToken);

        if (permissions.Length == 0)
        {
            return;
        }

        const string roleName = "System Administrators";
        var normalizedRoleName = Role.NormalizeNameKey(roleName);
        var role = await dbContext.Roles
            .Include(existingRole => existingRole.RolePermissions)
            .SingleOrDefaultAsync(existingRole => existingRole.NormalizedName == normalizedRoleName, cancellationToken);

        var hasChanges = false;

        if (role is null)
        {
            role = new Role(roleName, "Bootstrap platform administrators.");
            role.AssignPermissions(permissions);
            await dbContext.Roles.AddAsync(role, cancellationToken);
            hasChanges = true;
        }
        else
        {
            role.AssignPermissions(permissions);
            hasChanges = true;
        }

        var normalizedUsername = User.NormalizeUsernameKey(username);
        var user = await dbContext.Users
            .Include(existingUser => existingUser.UserRoles)
            .SingleOrDefaultAsync(existingUser => existingUser.NormalizedUsername == normalizedUsername, cancellationToken);

        var displayName = bootstrapSection["DisplayName"];
        var email = bootstrapSection["Email"];

        if (user is null)
        {
            user = new User(
                username,
                string.IsNullOrWhiteSpace(displayName) ? username : displayName,
                email,
                externalId: null,
                UserSourceType.Local,
                isActive: true,
                createdAtUtc: DateTimeOffset.UtcNow);
            user.SetLocalPassword(passwordHasher.HashPassword(password), DateTimeOffset.UtcNow);
            user.UserRoles.Add(new UserRole(user.Id, role.Id));

            await dbContext.Users.AddAsync(user, cancellationToken);
            hasChanges = true;
        }
        else
        {
            if (user.SourceType == UserSourceType.Local && !user.HasLocalPassword)
            {
                user.SetLocalPassword(passwordHasher.HashPassword(password), DateTimeOffset.UtcNow);
                hasChanges = true;
            }

            if (user.UserRoles.All(userRole => userRole.RoleId != role.Id))
            {
                user.UserRoles.Add(new UserRole(user.Id, role.Id));
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
