using System.Text.Json;
using System.Globalization;
using BG.Application;
using BG.Application.Approvals;
using BG.Infrastructure;
using BG.Infrastructure.Persistence;
using BG.Integrations;
using BG.Web.Configuration;
using BG.Web.Security;
using BG.Web.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var dataProtectionBuilder = builder.Services.AddDataProtection();
var dataProtectionApplicationName = builder.Configuration["DataProtection:ApplicationName"];
dataProtectionBuilder.SetApplicationName(
    string.IsNullOrWhiteSpace(dataProtectionApplicationName)
        ? "BG"
        : dataProtectionApplicationName.Trim());

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    var resolvedKeysPath = ProductionReadinessValidator.ResolveDirectoryPath(dataProtectionKeysPath);
    Directory.CreateDirectory(resolvedKeysPath);
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(resolvedKeysPath));
}

builder.Services.AddApplication();
builder.Services.Configure<ApprovalGovernanceOptions>(builder.Configuration.GetSection(ApprovalGovernanceOptions.SectionName));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIntegrations(builder.Configuration);
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IUiConfigurationService, UiConfigurationService>();
builder.Services.AddScoped<IWorkspaceShellService, WorkspaceShellService>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services
    .AddAuthentication(WorkspaceShellDefaults.AuthenticationScheme)
    .AddCookie(WorkspaceShellDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/SignIn";
        options.AccessDeniedPath = "/";
        options.Cookie.Name = "bg.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsProduction()
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
builder.Services.AddAuthorization(PermissionPolicyNames.Configure);
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.Configure<RequestLocalizationOptions>(options =>
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

var app = builder.Build();

ProductionReadinessValidator.Validate(app.Configuration, app.Environment);
await app.Services.InitializeInfrastructureAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<WorkspaceAccessMiddleware>();
app.UseAuthorization();

var swaggerEnabled = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled");

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapRazorPages();
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description
                })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
});

app.Run();

public partial class Program
{
}
