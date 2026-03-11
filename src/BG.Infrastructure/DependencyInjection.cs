using BG.Application.Contracts.Persistence;
using BG.Infrastructure.Persistence;
using BG.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BG.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSql")
            ?? "Host=localhost;Port=5432;Database=bg_app;Username=postgres;Password=change-me";

        services.AddDbContext<BgDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IIdentityAdministrationRepository, IdentityAdministrationRepository>();

        return services;
    }
}
