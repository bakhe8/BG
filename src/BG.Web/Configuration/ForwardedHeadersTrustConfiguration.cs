using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using AspNetCoreIPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace BG.Web.Configuration;

public static class ForwardedHeadersTrustConfiguration
{
    private const string SectionName = "ReverseProxy";

    public static void Apply(ForwardedHeadersOptions options, IConfiguration configuration)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        var knownProxies = ReadKnownProxies(configuration).ToArray();
        var knownNetworks = ReadKnownNetworks(configuration).ToArray();

        if (knownProxies.Length == 0 && knownNetworks.Length == 0)
        {
            return;
        }

        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();

        foreach (var knownProxy in knownProxies)
        {
            options.KnownProxies.Add(knownProxy);
        }

        foreach (var knownNetwork in knownNetworks)
        {
            options.KnownNetworks.Add(knownNetwork);
        }
    }

    public static bool HasTrustedProxyConfiguration(IConfiguration configuration)
    {
        return ReadKnownProxies(configuration).Any() || ReadKnownNetworks(configuration).Any();
    }

    public static IReadOnlyList<IPAddress> ReadKnownProxies(IConfiguration configuration)
    {
        return ReadValues(configuration, "KnownProxies")
            .Select(IPAddress.Parse)
            .ToArray();
    }

    public static IReadOnlyList<AspNetCoreIPNetwork> ReadKnownNetworks(IConfiguration configuration)
    {
        return ReadValues(configuration, "KnownNetworks")
            .Select(ParseNetwork)
            .ToArray();
    }

    private static IEnumerable<string> ReadValues(IConfiguration configuration, string key)
    {
        return configuration
            .GetSection($"{SectionName}:{key}")
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim());
    }

    private static AspNetCoreIPNetwork ParseNetwork(string value)
    {
        var segments = value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length != 2)
        {
            throw new InvalidOperationException($"Reverse proxy network '{value}' must use CIDR notation.");
        }

        return new AspNetCoreIPNetwork(IPAddress.Parse(segments[0]), int.Parse(segments[1]));
    }
}
