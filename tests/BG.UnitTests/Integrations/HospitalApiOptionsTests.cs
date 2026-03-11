using BG.Integrations.Options;

namespace BG.UnitTests.Integrations;

public sealed class HospitalApiOptionsTests
{
    [Fact]
    public void Defaults_match_the_current_integration_baseline()
    {
        var options = new HospitalApiOptions();

        Assert.Null(options.BaseUrl);
        Assert.Equal(30, options.TimeoutSeconds);
        Assert.Equal("ApiKey", options.AuthenticationMode);
        Assert.Equal("X-Api-Key", options.ApiKeyHeaderName);
        Assert.Null(options.ApiKey);
    }
}
