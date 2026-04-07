using BG.Application.Contracts.Services;
using BG.Application.Contracts.Services.Reports;
using BG.Application.Intake;
using BG.Application.Services;
using BG.Application.Services.Reports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using BG.Application.Approvals;

namespace BG.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<ApprovalGovernanceOptions>();
        AddOcrFallback(services);
        AddPlatformServices(services);
        AddIdentityServices(services);
        AddHomeServices(services);
        AddIntakeServices(services);
        AddRequestServices(services);
        AddApprovalServices(services);
        AddOperationsServices(services);
        AddDispatchServices(services);
        AddAdministrationServices(services);
        AddNotificationServices(services);
        AddReportServices(services);

        return services;
    }

    private static void AddOcrFallback(IServiceCollection services)
    {
        if (!services.Any(service => service.ServiceType == typeof(IOcrDocumentProcessingService)))
        {
            services.AddSingleton<IOcrDocumentProcessingService, NullOcrDocumentProcessingService>();
        }
    }

    private static void AddPlatformServices(IServiceCollection services)
    {
        services.AddSingleton<IArchitectureProfileService, ArchitectureProfileService>();
        services.TryAddScoped<IExecutionActorAccessor, NullExecutionActorAccessor>();
    }

    private static void AddIdentityServices(IServiceCollection services)
    {
        services.AddScoped<IIdentityAdministrationService, IdentityAdministrationService>();
        services.AddScoped<ILocalAuthenticationService, LocalAuthenticationService>();
        services.AddScoped<IUserAccessProfileService, UserAccessProfileService>();
    }

    private static void AddHomeServices(IServiceCollection services)
    {
        services.AddScoped<IHomeDashboardService, HomeDashboardService>();
    }

    private static void AddIntakeServices(IServiceCollection services)
    {
        services.AddScoped<IIntakeWorkspaceService, IntakeWorkspaceService>();
        services.AddScoped<IIntakeSubmissionService, IntakeSubmissionService>();
        services.AddSingleton<IIntakeDocumentClassifier, LocalIntakeDocumentClassifier>();
        services.AddSingleton<IIntakeDirectTextExtractor>(
            serviceProvider => new LocalIntakeDirectTextExtractor(
                serviceProvider.GetRequiredService<IOcrDocumentProcessingService>()));
        services.AddSingleton<IIntakeOcrExtractor>(
            serviceProvider => new LocalIntakeOcrExtractor(
                serviceProvider.GetRequiredService<IOcrDocumentProcessingService>()));
        services.AddSingleton<IIntakeExtractionConfidenceScorer, LocalIntakeExtractionConfidenceScorer>();
        services.AddSingleton<IIntakeCandidateValidator, LocalIntakeCandidateValidator>();
        services.AddSingleton<IIntakeFieldReviewProjector, LocalIntakeFieldReviewProjector>();
        services.AddSingleton<IIntakeExtractionEngine>(
            serviceProvider => new LocalIntakeExtractionEngine(
                serviceProvider.GetRequiredService<IIntakeDocumentClassifier>(),
                serviceProvider.GetRequiredService<IIntakeDirectTextExtractor>(),
                serviceProvider.GetRequiredService<IIntakeOcrExtractor>(),
                serviceProvider.GetRequiredService<IIntakeCandidateValidator>(),
                serviceProvider.GetRequiredService<IIntakeFieldReviewProjector>()));
    }

    private static void AddRequestServices(IServiceCollection services)
    {
        services.AddScoped<IRequestWorkspaceService, RequestWorkspaceService>();
        services.AddScoped<IGuaranteeHistoryService, GuaranteeHistoryService>();
    }

    private static void AddApprovalServices(IServiceCollection services)
    {
        services.AddScoped<IApprovalQueueService, ApprovalQueueService>();
        services.AddScoped<IApprovalDelegationAdministrationService, ApprovalDelegationAdministrationService>();
    }

    private static void AddOperationsServices(IServiceCollection services)
    {
        services.AddSingleton<IOperationsReviewMatchingService, OperationsReviewMatchingService>();
        services.AddScoped<IOperationsReviewQueueService, OperationsReviewQueueService>();
    }

    private static void AddDispatchServices(IServiceCollection services)
    {
        services.AddScoped<IDispatchWorkspaceService, DispatchWorkspaceService>();
    }

    private static void AddAdministrationServices(IServiceCollection services)
    {
        services.AddScoped<IBankAdministrationService, BankAdministrationService>();
        services.AddScoped<IBankLookupService, BankLookupService>();
        services.AddScoped<IWorkflowAdministrationService, WorkflowAdministrationService>();
        services.AddScoped<IWorkflowTemplateService, WorkflowTemplateService>();
    }

    private static void AddNotificationServices(IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
    }

    private static void AddReportServices(IServiceCollection services)
    {
        services.AddScoped<IReportService, ReportService>();
    }
}
