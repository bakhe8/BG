using BG.Application;
using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Models.Intake;
using Microsoft.Extensions.DependencyInjection;

namespace BG.UnitTests.Application;

public sealed class ApplicationDependencyInjectionTests
{
    [Fact]
    public void AddApplication_registers_architecture_profile_service_with_expected_baseline()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        using var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IArchitectureProfileService>();

        var profile = service.GetCurrent();

        Assert.Equal("BG", profile.ApplicationName);
        Assert.Equal("ASP.NET Core 8", profile.Framework);
        Assert.Equal("Razor Pages", profile.UserInterface);
        Assert.Equal("REST API Controllers", profile.ApiStyle);
        Assert.Equal("PostgreSQL", profile.Database);
        Assert.Equal("IIS", profile.Hosting);
        Assert.Equal("Internal Integration Layer", profile.IntegrationApproach);
    }

    [Fact]
    public void AddApplication_registers_identity_administration_service()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var descriptor = Assert.Single(services.Where(service => service.ServiceType == typeof(IIdentityAdministrationService)));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddApplication_registers_local_authentication_service()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var descriptor = Assert.Single(services.Where(service => service.ServiceType == typeof(ILocalAuthenticationService)));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddApplication_registers_home_dashboard_service()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var descriptor = Assert.Single(services.Where(service => service.ServiceType == typeof(IHomeDashboardService)));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddApplication_registers_intake_workspace_service()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var descriptor = Assert.Single(services.Where(service => service.ServiceType == typeof(IIntakeWorkspaceService)));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddApplication_registers_intake_extraction_engine()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var descriptor = Assert.Single(services.Where(service => service.ServiceType == typeof(IIntakeExtractionEngine)));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public async Task AddApplication_wires_custom_ocr_service_through_intake_extraction_engine()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOcrDocumentProcessingService>(
            new StubOcrDocumentProcessingService(
                new OcrDocumentProcessingResult(
                    true,
                    "stub-worker",
                    "wave2-text-first",
                    [
                        new OcrDocumentFieldCandidateDto(
                            IntakeFieldKeys.GuaranteeNumber,
                            "BG-2026-3131",
                            99,
                            1,
                            "auto",
                            "direct-pdf-text")
                    ],
                    [],
                    null,
                    null)));
        services.AddApplication();

        using var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IIntakeExtractionEngine>();
        var stagedPath = Path.Combine(Path.GetTempPath(), $"bg-di-{Guid.NewGuid():N}.pdf");
        await File.WriteAllTextAsync(stagedPath, "placeholder");

        try
        {
            var draft = await engine.ExtractAsync(
                IntakeScenarioKeys.ExtensionConfirmation,
                new StagedIntakeDocumentDto("token-1", "extension-letter.pdf", 128, stagedPath));

            Assert.Contains(draft.Fields, field => field.FieldKey == IntakeFieldKeys.GuaranteeNumber && field.Value == "BG-2026-3131");
        }
        finally
        {
            File.Delete(stagedPath);
        }
    }

    [Fact]
    public void AddApplication_registers_intake_submission_service()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var descriptor = Assert.Single(services.Where(service => service.ServiceType == typeof(IIntakeSubmissionService)));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddApplication_registers_operations_review_queue_service()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var descriptor = Assert.Single(services.Where(service => service.ServiceType == typeof(IOperationsReviewQueueService)));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddApplication_registers_request_workspace_service()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var descriptor = Assert.Single(services.Where(service => service.ServiceType == typeof(IRequestWorkspaceService)));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddApplication_registers_dispatch_workspace_service()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var descriptor = Assert.Single(services.Where(service => service.ServiceType == typeof(IDispatchWorkspaceService)));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    private sealed class StubOcrDocumentProcessingService : IOcrDocumentProcessingService
    {
        private readonly OcrDocumentProcessingResult _result;

        public StubOcrDocumentProcessingService(OcrDocumentProcessingResult result)
        {
            _result = result;
        }

        public Task<OcrDocumentProcessingResult> ProcessAsync(
            OcrDocumentProcessingRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
    }
}
