using BG.Application.Security;
using BG.Domain.Identity;
using Microsoft.EntityFrameworkCore;
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

        await dbContext.Database.MigrateAsync(cancellationToken);

        var existingKeys = await dbContext.Permissions
            .Select(permission => permission.Key)
            .ToListAsync(cancellationToken);

        var missingPermissions = PermissionCatalog.Definitions
            .Where(definition => !existingKeys.Contains(definition.Key, StringComparer.OrdinalIgnoreCase))
            .Select(definition => new Permission(definition.Key, definition.Area))
            .ToArray();

        if (missingPermissions.Length == 0)
        {
            return;
        }

        await dbContext.Permissions.AddRangeAsync(missingPermissions, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
