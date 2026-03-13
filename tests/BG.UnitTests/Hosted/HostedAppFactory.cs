using System.Data.Common;
using BG.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BG.UnitTests.Hosted;

internal sealed class HostedAppFactory : WebApplicationFactory<Program>
{
    private const string HostedPostgreSqlConnectionString = "Host=127.0.0.1;Port=5432;Database=bg_hosted_tests;Username=bg_tests;Password=HostedTests123!";
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;

    public HostedAppFactory(IReadOnlyDictionary<string, string?>? configurationOverrides = null)
    {
        _configurationOverrides = configurationOverrides ?? new Dictionary<string, string?>();
        _connection.Open();
    }

    public HttpClient CreateAppClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    public async Task ExecuteDbContextAsync(Func<BgDbContext, IServiceProvider, Task> action)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BgDbContext>();
        await action(dbContext, scope.ServiceProvider);
        await dbContext.SaveChangesAsync();
    }

    public async Task<T> QueryDbContextAsync<T>(Func<BgDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BgDbContext>();
        return await query(dbContext);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSql"] = HostedPostgreSqlConnectionString,
                ["Identity:BootstrapAdmin:Username"] = "hosted.admin",
                ["Identity:BootstrapAdmin:DisplayName"] = "Hosted Admin",
                ["Identity:BootstrapAdmin:Email"] = "hosted.admin@bg.local",
                ["Identity:BootstrapAdmin:Password"] = "HostedAdmin123!"
            };

            foreach (var (key, value) in _configurationOverrides)
            {
                values[key] = value;
            }

            configurationBuilder.AddInMemoryCollection(values);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<BgDbContext>));
            services.RemoveAll(typeof(BgDbContext));
            services.RemoveAll<DbConnection>();

            services.AddSingleton<DbConnection>(_connection);
            services.AddDbContext<BgDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite(serviceProvider.GetRequiredService<DbConnection>());
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
