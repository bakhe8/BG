using BG.Infrastructure.PostgreSql;
using Microsoft.Extensions.Configuration;

namespace BG.UnitTests.Infrastructure;

public sealed class PostgreSqlConnectionStringResolverTests
{
    [Fact]
    public void NormalizeAndValidate_returns_hardened_connection_string()
    {
        const string connectionString = "Host=127.0.0.1;Port=5432;Database=bg_app;Username=bg_app;Password=secret-value";

        var normalized = PostgreSqlConnectionStringResolver.NormalizeAndValidate(connectionString);
        var builder = new System.Data.Common.DbConnectionStringBuilder
        {
            ConnectionString = normalized
        };

        Assert.Equal("127.0.0.1", builder["Host"]);
        Assert.Equal("5432", builder["Port"].ToString());
        Assert.Equal("bg_app", builder["Database"]);
        Assert.Equal("bg_app", builder["Username"]);
        Assert.Equal("BG", builder["Application Name"]);
    }

    [Fact]
    public void NormalizeAndValidate_throws_for_placeholder_password()
    {
        const string connectionString = "Host=127.0.0.1;Port=5432;Database=bg_app;Username=bg_app;Password=change-me";

        var exception = Assert.Throws<InvalidOperationException>(
            () => PostgreSqlConnectionStringResolver.NormalizeAndValidate(connectionString));

        Assert.Contains("ConnectionStrings:PostgreSql", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_falls_back_to_environment_variable_when_configured_connection_uses_placeholder_password()
    {
        const string environmentConnectionString = "Host=127.0.0.1;Port=5432;Database=bg_app;Username=bg_app;Password=secret-value";
        var originalEnvironmentConnectionString = Environment.GetEnvironmentVariable("BG_POSTGRESQL_CONNECTION");

        try
        {
            Environment.SetEnvironmentVariable("BG_POSTGRESQL_CONNECTION", environmentConnectionString);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:PostgreSql"] = "Host=127.0.0.1;Port=5432;Database=bg_app;Username=bg_app;Password=change-me"
                    })
                .Build();

            var resolved = PostgreSqlConnectionStringResolver.Resolve(configuration);
            var builder = new System.Data.Common.DbConnectionStringBuilder
            {
                ConnectionString = resolved
            };

            Assert.Equal("secret-value", builder["Password"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BG_POSTGRESQL_CONNECTION", originalEnvironmentConnectionString);
        }
    }
}
