using BG.Web.Security;
using BG.Web.UI;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace BG.Web.Configuration;

public static class BgMiddlewareExtensions
{
    public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseBgSecurity(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            app.UseExceptionHandler(exceptionHandlerApp =>
            {
                exceptionHandlerApp.Run(async context =>
                {
                    var logger = context.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger("Program");
                    var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
                    var traceId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;

                    if (exceptionHandlerPathFeature?.Error != null)
                    {
                        logger.LogError(exceptionHandlerPathFeature.Error, "[TRACE:{TraceId}] Sanitized exception occurred at {Path}", traceId, exceptionHandlerPathFeature.Path);
                    }

                    context.Response.Redirect($"/Error?requestId={traceId}");
                });
            });
            app.UseHsts();
        }

        app.UseAuthentication();
        app.UseMiddleware<BG.Web.UI.WorkspaceAccessMiddleware>();
        app.UseAuthorization();

        return app;
    }

    public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseBgWebDefaults(this Microsoft.AspNetCore.Builder.IApplicationBuilder app)
    {
        app.UseForwardedHeaders();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRequestLocalization(app.ApplicationServices.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

        app.UseRouting();

        return app;
    }

    public static Microsoft.AspNetCore.Routing.IEndpointRouteBuilder MapBgEndpoints(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
    {
        endpoints.MapRazorPages();
        endpoints.MapControllers();
        endpoints.MapHub<NotificationHub>("/notificationHub");

        endpoints.MapHealthChecks("/health", new HealthCheckOptions
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

        return endpoints;
    }
}
