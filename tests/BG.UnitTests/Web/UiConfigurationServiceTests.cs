using BG.Web.UI;
using Microsoft.AspNetCore.Http;

namespace BG.UnitTests.Web;

public sealed class UiConfigurationServiceTests
{
    [Fact]
    public void Normalization_uses_supported_languages_and_themes_only()
    {
        var service = CreateService();

        Assert.Equal("ar", service.DefaultCulture);
        Assert.Equal("kfsh", service.DefaultTheme);
        Assert.Equal("ar", service.NormalizeCulture("ar-SA"));
        Assert.Equal("en", service.NormalizeCulture("en-US"));
        Assert.Equal("ar", service.NormalizeCulture("fr"));
        Assert.Equal("kfsh", service.NormalizeTheme("Slate"));
        Assert.Equal("kfsh", service.NormalizeTheme("unknown"));
        Assert.Equal("rtl", service.GetDirection("ar"));
        Assert.Equal("ltr", service.GetDirection("en"));
    }

    [Fact]
    public void GetCurrentTheme_reads_cookie_and_falls_back_to_default()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = $"{UiConfigurationService.ThemeCookieName}=sand";
        var service = CreateService(httpContext);

        Assert.Equal("kfsh", service.GetCurrentTheme());
    }

    private static UiConfigurationService CreateService(HttpContext? httpContext = null)
    {
        return new UiConfigurationService(new HttpContextAccessor
        {
            HttpContext = httpContext ?? new DefaultHttpContext()
        });
    }
}
