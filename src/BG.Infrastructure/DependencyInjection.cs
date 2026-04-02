using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Persistence.Notifications;
using BG.Application.Contracts.Services;
using BG.Infrastructure.HealthChecks;
using BG.Infrastructure.Persistence;
using BG.Infrastructure.Persistence.Repositories;
using BG.Infrastructure.PostgreSql;
using BG.Infrastructure.Security;
using BG.Infrastructure.Storage;
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
        services.AddDbContext<BgDbContext>((serviceProvider, options) =>
        {
            var runtimeConfiguration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = PostgreSqlConnectionStringResolver.Resolve(runtimeConfiguration);

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null));
        });
        services.AddHealthChecks()
            .AddCheck<PostgreSqlHealthCheck>("postgresql");
        services.AddScoped<IIdentityAdministrationRepository, IdentityAdministrationRepository>();
        services.AddScoped<IIntakeRepository, IntakeRepository>();
        services.AddScoped<IHomeDashboardRepository, HomeDashboardRepository>();
        services.AddScoped<IOperationsReviewRepository, OperationsReviewRepository>();
        services.AddScoped<IRequestWorkspaceRepository, RequestWorkspaceRepository>();
        services.AddScoped<IApprovalQueueRepository, ApprovalQueueRepository>();
        services.AddScoped<IDispatchWorkspaceRepository, DispatchWorkspaceRepository>();
        services.AddScoped<IWorkflowDefinitionRepository, WorkflowDefinitionRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IIntakeDocumentStore, LocalIntakeDocumentStore>();
        services.AddSingleton<ILocalPasswordHasher, Pbkdf2LocalPasswordHasher>();
        services.AddScoped<OperationalSeedService>();

        return services;
    }
}
