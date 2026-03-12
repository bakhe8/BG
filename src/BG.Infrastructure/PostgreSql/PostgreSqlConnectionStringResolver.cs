using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BG.Infrastructure.PostgreSql;

public static class PostgreSqlConnectionStringResolver
{
    private const string PlaceholderPassword = "change-me";
    private const string DefaultConnectionString = "Host=127.0.0.1;Port=5432;Database=bg_app;Username=bg_app;Password=change-me";

    public static string Resolve(IConfiguration configuration)
    {
        var configuredConnectionString = configuration.GetConnectionString("PostgreSql");
        if (HasUsablePassword(configuredConnectionString))
        {
            return NormalizeAndValidate(configuredConnectionString!);
        }

        var environmentConnectionString = Environment.GetEnvironmentVariable("BG_POSTGRESQL_CONNECTION");
        if (HasUsablePassword(environmentConnectionString))
        {
            return NormalizeAndValidate(environmentConnectionString!);
        }

        var fallbackConnectionString = configuredConnectionString
            ?? environmentConnectionString
            ?? DefaultConnectionString;

        return NormalizeAndValidate(fallbackConnectionString);
    }

    public static string NormalizeAndValidate(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw BuildMissingConnectionStringException();
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        if (string.IsNullOrWhiteSpace(builder.Password) ||
            string.Equals(builder.Password, PlaceholderPassword, StringComparison.Ordinal))
        {
            throw BuildMissingConnectionStringException();
        }

        if (string.IsNullOrWhiteSpace(builder.Host))
        {
            builder.Host = "127.0.0.1";
        }

        if (builder.Port == 0)
        {
            builder.Port = 5432;
        }

        if (string.IsNullOrWhiteSpace(builder.Database))
        {
            builder.Database = "bg_app";
        }

        if (string.IsNullOrWhiteSpace(builder.Username))
        {
            builder.Username = "bg_app";
        }

        if (string.IsNullOrWhiteSpace(builder.ApplicationName))
        {
            builder.ApplicationName = "BG";
        }

        builder.Pooling = true;

        return builder.ConnectionString;
    }

    private static InvalidOperationException BuildMissingConnectionStringException()
    {
        return new InvalidOperationException(
            "PostgreSQL connection is not configured. Set ConnectionStrings:PostgreSql via `dotnet user-secrets --project src/BG.Web` for local development, or provide it through environment variables / IIS configuration.");
    }

    private static bool HasUsablePassword(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        return !string.IsNullOrWhiteSpace(builder.Password)
            && !string.Equals(builder.Password, PlaceholderPassword, StringComparison.Ordinal);
    }
}
