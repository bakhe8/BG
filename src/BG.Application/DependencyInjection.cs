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
        services.AddSingleton<IArchitectureProfileService, ArchitectureProfileService>();
        services.AddScoped<IIntakeWorkspaceService, IntakeWorkspaceService>();
        services.AddSingleton<IIntakeDocumentClassifier, LocalIntakeDocumentClassifier>();
        services.AddSingleton<IIntakeDirectTextExtractor, LocalIntakeDirectTextExtractor>();
        services.AddSingleton<IIntakeOcrExtractor, LocalIntakeOcrExtractor>();
        services.AddSingleton<IIntakeExtractionConfidenceScorer, LocalIntakeExtractionConfidenceScorer>();
        services.AddSingleton<IIntakeFieldReviewProjector, LocalIntakeFieldReviewProjector>();
        services.AddSingleton<IIntakeExtractionEngine, LocalIntakeExtractionEngine>();
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
