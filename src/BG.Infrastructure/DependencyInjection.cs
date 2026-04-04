using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Persistence.Notifications;
using BG.Application.Contracts.Persistence.Reports;
using BG.Application.Contracts.Services;
using BG.Infrastructure.Documents;
using BG.Infrastructure.Email;
using BG.Infrastructure.HealthChecks;
using BG.Infrastructure.Identity;
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
            var sqliteConnection = runtimeConfiguration.GetConnectionString("Sqlite");
            
            if (!string.IsNullOrWhiteSpace(sqliteConnection))
            {
                options.UseSqlite(sqliteConnection);
            }
            else
            {
                var connectionString = PostgreSqlConnectionStringResolver.Resolve(runtimeConfiguration);
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null));
            }
        });
        services.AddHealthChecks()
            .AddCheck<PostgreSqlHealthCheck>("postgresql");
        services.AddScoped<IIdentityAdministrationRepository, IdentityAdministrationRepository>();
        services.AddScoped<IIntakeRepository, IntakeRepository>();
        services.AddScoped<IHomeDashboardRepository, HomeDashboardRepository>();
        services.AddScoped<IOperationsReviewRepository, OperationsReviewRepository>();
        services.AddScoped<IRequestWorkspaceRepository, RequestWorkspaceRepository>();
        services.AddScoped<IGuaranteeHistoryRepository, GuaranteeHistoryRepository>();
        services.AddScoped<IBankRepository, BankRepository>();
        services.AddScoped<IApprovalQueueRepository, ApprovalQueueRepository>();
        services.AddScoped<IDispatchWorkspaceRepository, DispatchWorkspaceRepository>();
        services.AddScoped<IWorkflowDefinitionRepository, WorkflowDefinitionRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<ILetterGenerationService, QuestPdfLetterGenerationService>();
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddScoped<IEmailDispatchService, MailKitEmailDispatchService>();
        services.AddSingleton<ISmtpClientAdapterFactory, MailKitSmtpClientAdapterFactory>();
        services.AddScoped<IIntakeDocumentStore, LocalIntakeDocumentStore>();
        services.AddHostedService<StagingCleanupService>();
        services.AddSingleton<ILocalPasswordHasher, Pbkdf2LocalPasswordHasher>();
        services.Configure<BG.Application.Contracts.Services.LoginLockoutOptions>(
            configuration.GetSection(BG.Application.Contracts.Services.LoginLockoutOptions.SectionName));
        services.AddScoped<ILoginAttemptLockoutService, DbLoginAttemptLockoutService>();
        services.AddScoped<OperationalSeedService>();

        return services;
    }
}
