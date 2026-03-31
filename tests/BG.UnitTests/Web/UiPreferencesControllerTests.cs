using BG.Web.Controllers;
using BG.Web.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BG.UnitTests.Web;

public sealed class UiPreferencesControllerTests
{
    [Fact]
    public void SetTheme_normalizes_theme_writes_cookie_and_redirects_locally()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        var controller = CreateController(httpContext);

        var result = controller.SetTheme("SLATE", "/dashboard");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/dashboard", redirect.Url);
        var cookieHeader = httpContext.Response.Headers.SetCookie.ToString();
        Assert.Contains("bg-theme=kfsh", cookieHeader);
        Assert.Contains("httponly", cookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookieHeader, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SetCulture_rejects_external_return_urls()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        var controller = CreateController(httpContext);

        var result = controller.SetCulture("en-US", "https://example.com/outside");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/", redirect.Url);
        var cookieHeader = httpContext.Response.Headers.SetCookie.ToString();
        Assert.Contains(".AspNetCore.Culture", cookieHeader);
        Assert.Contains("c%3Den", cookieHeader);
        Assert.Contains("httponly", cookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookieHeader, StringComparison.OrdinalIgnoreCase);
    }

    private static UiPreferencesController CreateController(HttpContext httpContext)
    {
        var controller = new UiPreferencesController(
            new UiConfigurationService(new HttpContextAccessor { HttpContext = httpContext }))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        return controller;
    }
}
