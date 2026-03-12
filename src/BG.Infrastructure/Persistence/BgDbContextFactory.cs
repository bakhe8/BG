using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using BG.Infrastructure.PostgreSql;

namespace BG.Infrastructure.Persistence;

public sealed class BgDbContextFactory : IDesignTimeDbContextFactory<BgDbContext>
{
    public BgDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BgDbContext>();
        var userSecretsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft",
            "UserSecrets",
            "BG.Web-c0b80b51-8c14-4304-a3db-79ebd28d4d24",
            "secrets.json");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(ResolveWebProjectDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile(userSecretsPath, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = PostgreSqlConnectionStringResolver.Resolve(configuration);

        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null));

        return new BgDbContext(optionsBuilder.Options);
    }

    private static string ResolveWebProjectDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "src", "BG.Web"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "BG.Web")
        };

        foreach (var candidate in candidates.Select(Path.GetFullPath))
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Unable to locate the BG.Web project directory for design-time configuration.");
    }
}
