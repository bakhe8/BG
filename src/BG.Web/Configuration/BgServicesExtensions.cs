using System.Globalization;
using BG.Application;
using BG.Application.Approvals;
using BG.Infrastructure;
using BG.Integrations;
using BG.Web.Security;
using BG.Web.UI;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using BG.Application.Contracts.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

namespace BG.Web.Configuration;

public static class BgServicesExtensions
{
    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddBgSecurity(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration, Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment)
    {
        var dataProtectionBuilder = services.AddDataProtection();
        var dataProtectionApplicationName = configuration["DataProtection:ApplicationName"];
        dataProtectionBuilder.SetApplicationName(
            string.IsNullOrWhiteSpace(dataProtectionApplicationName)
                ? "BG"
                : dataProtectionApplicationName.Trim());

        var dataProtectionKeysPath = configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
        {
            var resolvedKeysPath = ProductionReadinessValidator.ResolveDirectoryPath(dataProtectionKeysPath);
            if (!Directory.Exists(resolvedKeysPath))
            {
                Directory.CreateDirectory(resolvedKeysPath);
            }
            
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(resolvedKeysPath));
        }

        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services
            .AddAuthentication(WorkspaceShellDefaults.AuthenticationScheme)
            .AddCookie(WorkspaceShellDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/SignIn";
                options.AccessDeniedPath = "/";
                options.Cookie.Name = "bg.auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = environment.IsProduction()
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(12);
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
                        context.Response.Redirect($"/SignIn?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}&shellMessage=WorkspaceShell_SignInRequired");
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = context =>
                    {
                        context.Response.Redirect("/?shellMessage=WorkspaceShell_AccessDenied");
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(PermissionPolicyNames.Configure);

        return services;
    }

    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddBgProjectServices(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.AddApplication();
        services.Configure<ApprovalGovernanceOptions>(configuration.GetSection(ApprovalGovernanceOptions.SectionName));
        services.AddInfrastructure(configuration);
        services.AddIntegrations(configuration);

        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddScoped<IUiConfigurationService, UiConfigurationService>();
        services.AddScoped<IWorkspaceShellService, WorkspaceShellService>();
        services.AddScoped<IExecutionActorAccessor, HttpContextExecutionActorAccessor>();
        services.AddScoped<INotificationBroadcaster, NotificationBroadcaster>();

        return services;
    }

    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddBgWebDefaults(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddSignalR();
        services.AddRazorPages();
        services.AddControllers();
        services.AddProblemDetails();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            ForwardedHeadersTrustConfiguration.Apply(options, configuration);
        });

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("ar"),
                new CultureInfo("en")
            };

            options.DefaultRequestCulture = new RequestCulture(UiConfigurationService.DefaultCultureName);
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            options.RequestCultureProviders =
            [
                new QueryStringRequestCultureProvider(),
                new CookieRequestCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider()
            ];
        });

        return services;
    }
}
