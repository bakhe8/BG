using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace BG.Web.Configuration;

public static class ProductionReadinessValidator
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var failures = new List<string>();

        ValidateAllowedHosts(configuration, failures);
        ValidateStorage(configuration, failures);
        ValidateDataProtection(configuration, failures);
        ValidateReverseProxyTrust(configuration, failures);
        ValidateOperationalSeed(configuration, failures);
        ValidateSwagger(configuration, failures);
        ValidateOcr(configuration, failures);

        if (failures.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "Production configuration validation failed:" + Environment.NewLine +
            string.Join(Environment.NewLine, failures.Select(failure => $"- {failure}")));
    }

    public static string ResolveFilePath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
    }

    public static string ResolveDirectoryPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
    }

    private static void ValidateAllowedHosts(IConfiguration configuration, ICollection<string> failures)
    {
        var allowedHosts = configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts))
        {
            failures.Add("AllowedHosts must be explicitly configured for production.");
            return;
        }

        var hosts = allowedHosts
            .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (hosts.Length == 0 || hosts.Any(host => host == "*"))
        {
            failures.Add("AllowedHosts must not be blank and cannot contain '*'.");
        }
    }

    private static void ValidateStorage(IConfiguration configuration, ICollection<string> failures)
    {
        var documentsRoot = configuration["Storage:DocumentsRoot"];
        if (string.IsNullOrWhiteSpace(documentsRoot))
        {
            failures.Add("Storage:DocumentsRoot must be configured for production.");
            return;
        }

        try
        {
            Directory.CreateDirectory(ResolveDirectoryPath(documentsRoot));
        }
        catch (Exception exception)
        {
            failures.Add($"Storage:DocumentsRoot could not be prepared: {exception.Message}");
        }
    }

    private static void ValidateDataProtection(IConfiguration configuration, ICollection<string> failures)
    {
        var keysPath = configuration["DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(keysPath))
        {
            failures.Add("DataProtection:KeysPath must be configured for production.");
            return;
        }

        try
        {
            Directory.CreateDirectory(ResolveDirectoryPath(keysPath));
        }
        catch (Exception exception)
        {
            failures.Add($"DataProtection:KeysPath could not be prepared: {exception.Message}");
        }
    }

    private static void ValidateReverseProxyTrust(IConfiguration configuration, ICollection<string> failures)
    {
        try
        {
            if (!ForwardedHeadersTrustConfiguration.HasTrustedProxyConfiguration(configuration))
            {
                failures.Add("ReverseProxy:KnownProxies or ReverseProxy:KnownNetworks must be configured for production.");
            }
        }
        catch (Exception exception)
        {
            failures.Add($"Reverse proxy trust configuration is invalid: {exception.Message}");
        }
    }

    private static void ValidateOperationalSeed(IConfiguration configuration, ICollection<string> failures)
    {
        if (configuration.GetValue<bool>("OperationalSeed:Enabled"))
        {
            failures.Add("OperationalSeed:Enabled must remain false in production.");
        }
    }

    private static void ValidateSwagger(IConfiguration configuration, ICollection<string> failures)
    {
        if (configuration.GetValue<bool>("Swagger:Enabled"))
        {
            failures.Add("Swagger:Enabled must remain false in production.");
        }
    }

    private static void ValidateOcr(IConfiguration configuration, ICollection<string> failures)
    {
        if (!configuration.GetValue<bool>("Ocr:Enabled"))
        {
            return;
        }

        var pythonExecutablePath = configuration["Ocr:PythonExecutablePath"];
        if (string.IsNullOrWhiteSpace(pythonExecutablePath))
        {
            failures.Add("Ocr:PythonExecutablePath must be configured when OCR is enabled in production.");
        }
        else if (!File.Exists(ResolveFilePath(pythonExecutablePath)))
        {
            failures.Add("Ocr:PythonExecutablePath does not exist.");
        }

        var workerScriptPath = configuration["Ocr:WorkerScriptPath"];
        if (string.IsNullOrWhiteSpace(workerScriptPath))
        {
            failures.Add("Ocr:WorkerScriptPath must be configured when OCR is enabled in production.");
        }
        else if (!File.Exists(ResolveFilePath(workerScriptPath)))
        {
            failures.Add("Ocr:WorkerScriptPath does not exist.");
        }

        var maxFileSizeBytes = configuration.GetValue<long>("Ocr:MaxFileSizeBytes");
        if (maxFileSizeBytes <= 0)
        {
            failures.Add("Ocr:MaxFileSizeBytes must be greater than zero when OCR is enabled in production.");
        }

        var queueCapacity = configuration.GetValue<int>("Ocr:QueueCapacity");
        if (queueCapacity <= 0)
        {
            failures.Add("Ocr:QueueCapacity must be greater than zero when OCR is enabled in production.");
        }
    }
}
