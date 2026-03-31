using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using BG.Application.Approvals;

namespace BG.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<ApprovalGovernanceOptions>();
        if (!services.Any(service => service.ServiceType == typeof(IOcrDocumentProcessingService)))
        {
            services.AddSingleton<IOcrDocumentProcessingService, NullOcrDocumentProcessingService>();
        }
        services.AddSingleton<IArchitectureProfileService, ArchitectureProfileService>();
        services.AddScoped<IIntakeWorkspaceService, IntakeWorkspaceService>();
        services.AddSingleton<IIntakeDocumentClassifier, LocalIntakeDocumentClassifier>();
        services.AddSingleton<IIntakeDirectTextExtractor>(
            serviceProvider => new LocalIntakeDirectTextExtractor(
                serviceProvider.GetRequiredService<IOcrDocumentProcessingService>()));
        services.AddSingleton<IIntakeOcrExtractor>(
            serviceProvider => new LocalIntakeOcrExtractor(
                serviceProvider.GetRequiredService<IOcrDocumentProcessingService>()));
        services.AddSingleton<IIntakeExtractionConfidenceScorer, LocalIntakeExtractionConfidenceScorer>();
        services.AddSingleton<IIntakeFieldReviewProjector, LocalIntakeFieldReviewProjector>();
        services.AddSingleton<IIntakeExtractionEngine>(
            serviceProvider => new LocalIntakeExtractionEngine(
                serviceProvider.GetRequiredService<IIntakeDocumentClassifier>(),
                serviceProvider.GetRequiredService<IIntakeDirectTextExtractor>(),
                serviceProvider.GetRequiredService<IIntakeOcrExtractor>(),
                serviceProvider.GetRequiredService<IIntakeFieldReviewProjector>()));
        services.AddSingleton<IOperationsReviewMatchingService, OperationsReviewMatchingService>();
        services.AddScoped<IIdentityAdministrationService, IdentityAdministrationService>();
        services.AddScoped<ILocalAuthenticationService, LocalAuthenticationService>();
        services.AddScoped<IUserAccessProfileService, UserAccessProfileService>();
        services.AddScoped<IHomeDashboardService, HomeDashboardService>();
        services.AddScoped<IIntakeSubmissionService, IntakeSubmissionService>();
        services.AddScoped<IOperationsReviewQueueService, OperationsReviewQueueService>();
        services.AddScoped<IRequestWorkspaceService, RequestWorkspaceService>();
        services.AddScoped<IApprovalQueueService, ApprovalQueueService>();
        services.AddScoped<IApprovalDelegationAdministrationService, ApprovalDelegationAdministrationService>();
        services.AddScoped<IDispatchWorkspaceService, DispatchWorkspaceService>();
        services.AddScoped<IWorkflowAdministrationService, WorkflowAdministrationService>();
        services.AddScoped<IWorkflowTemplateService, WorkflowTemplateService>();

        return services;
    }
}
