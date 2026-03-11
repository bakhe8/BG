using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BG.Infrastructure.Persistence;

public sealed class BgDbContextFactory : IDesignTimeDbContextFactory<BgDbContext>
{
    public BgDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BgDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("BG_POSTGRESQL_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=bg_app;Username=postgres;Password=change-me";

        optionsBuilder.UseNpgsql(connectionString);

        return new BgDbContext(optionsBuilder.Options);
    }
}
