using BG.Web.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace BG.UnitTests.Web;

public sealed class ProductionReadinessValidatorTests
{
    [Fact]
    public void Validate_skips_strict_checks_outside_production()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AllowedHosts"] = "*"
        });

        ProductionReadinessValidator.Validate(configuration, new StubHostEnvironment("Development"));
    }

    [Fact]
    public void Validate_rejects_wildcard_allowed_hosts_in_production()
    {
        var paths = CreateProductionPaths();

        try
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["AllowedHosts"] = "*",
                ["Storage:DocumentsRoot"] = paths.DocumentsRoot,
                ["DataProtection:KeysPath"] = paths.KeysRoot
            });

            var exception = Assert.Throws<InvalidOperationException>(
                () => ProductionReadinessValidator.Validate(configuration, new StubHostEnvironment("Production")));

            Assert.Contains("AllowedHosts", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            paths.Dispose();
        }
    }

    [Fact]
    public void Validate_rejects_enabled_operational_seed_in_production()
    {
        var paths = CreateProductionPaths();

        try
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["AllowedHosts"] = "bg.example.internal",
                ["Storage:DocumentsRoot"] = paths.DocumentsRoot,
                ["DataProtection:KeysPath"] = paths.KeysRoot,
                ["OperationalSeed:Enabled"] = "true"
            });

            var exception = Assert.Throws<InvalidOperationException>(
                () => ProductionReadinessValidator.Validate(configuration, new StubHostEnvironment("Production")));

            Assert.Contains("OperationalSeed", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            paths.Dispose();
        }
    }

    [Fact]
    public void Validate_rejects_missing_ocr_runtime_when_enabled_in_production()
    {
        var paths = CreateProductionPaths();

        try
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["AllowedHosts"] = "bg.example.internal",
                ["Storage:DocumentsRoot"] = paths.DocumentsRoot,
                ["DataProtection:KeysPath"] = paths.KeysRoot,
                ["Ocr:Enabled"] = "true",
                ["Ocr:PythonExecutablePath"] = Path.Combine(paths.Root, "missing-python.exe"),
                ["Ocr:WorkerScriptPath"] = Path.Combine(paths.Root, "missing-worker.py")
            });

            var exception = Assert.Throws<InvalidOperationException>(
                () => ProductionReadinessValidator.Validate(configuration, new StubHostEnvironment("Production")));

            Assert.Contains("Ocr:PythonExecutablePath", exception.Message, StringComparison.Ordinal);
            Assert.Contains("Ocr:WorkerScriptPath", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            paths.Dispose();
        }
    }

    [Fact]
    public void Validate_accepts_explicit_production_configuration()
    {
        var paths = CreateProductionPaths(includeOcrRuntime: true);

        try
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["AllowedHosts"] = "bg.example.internal",
                ["Storage:DocumentsRoot"] = paths.DocumentsRoot,
                ["DataProtection:KeysPath"] = paths.KeysRoot,
                ["Swagger:Enabled"] = "false",
                ["OperationalSeed:Enabled"] = "false",
                ["Ocr:Enabled"] = "true",
                ["Ocr:PythonExecutablePath"] = paths.PythonExecutablePath,
                ["Ocr:WorkerScriptPath"] = paths.WorkerScriptPath
            });

            ProductionReadinessValidator.Validate(configuration, new StubHostEnvironment("Production"));
        }
        finally
        {
            paths.Dispose();
        }
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static ProductionPaths CreateProductionPaths(bool includeOcrRuntime = false)
    {
        var root = Path.Combine(Path.GetTempPath(), $"bg-prod-{Guid.NewGuid():N}");
        var documentsRoot = Path.Combine(root, "documents");
        var keysRoot = Path.Combine(root, "keys");
        Directory.CreateDirectory(documentsRoot);
        Directory.CreateDirectory(keysRoot);

        string? pythonExecutablePath = null;
        string? workerScriptPath = null;

        if (includeOcrRuntime)
        {
            pythonExecutablePath = Path.Combine(root, "python.exe");
            workerScriptPath = Path.Combine(root, "ocr_worker.py");
            File.WriteAllText(pythonExecutablePath, string.Empty);
            File.WriteAllText(workerScriptPath, "# worker");
        }

        return new ProductionPaths(root, documentsRoot, keysRoot, pythonExecutablePath, workerScriptPath);
    }

    private sealed record ProductionPaths(
        string Root,
        string DocumentsRoot,
        string KeysRoot,
        string? PythonExecutablePath,
        string? WorkerScriptPath) : IDisposable
    {
        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public StubHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "BG";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
