using System.Text;
using System.Text.Json;
using BG.Application.Intake;
using BG.Application.Models.Intake;
using BG.Application.ReferenceData;
using BG.Integrations;
using BG.Integrations.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BG.UnitTests.Integrations;

public sealed class LocalPythonOcrProcessingServiceTests
{
    private static readonly JsonSerializerOptions TruthJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task ProcessAsync_extracts_structured_fields_from_text_pdf_when_python_worker_is_available()
    {
        using var harness = OcrTestHarness.Create();

        var pdfPath = harness.CreateSimpleTextPdf("BG-2026-1234 EXT-ABC-999 2026-03-10");

        try
        {
            var result = await harness.Service.ProcessAsync(
                CreateRequest(
                    pdfPath,
                    "extension-letter.pdf",
                    IntakeScenarioKeys.ExtensionConfirmation,
                    GuaranteeDocumentFormKeys.BankLetterGeneric,
                    GuaranteeDocumentBankProfileKeys.Generic,
                    GuaranteeDocumentFormStructuralClassKeys.AmendmentLetter,
                    "Generic Bank",
                    "EXT"));

            Assert.True(result.Succeeded);
            Assert.Equal("wave2-text-first", result.PipelineVersion);
            Assert.Contains(result.Fields, field => field.FieldKey == IntakeFieldKeys.GuaranteeNumber && field.Value == "BG-2026-1234");
            Assert.Contains(result.Fields, field => field.FieldKey == IntakeFieldKeys.BankReference && field.Value == "EXT-ABC-999");
            Assert.Contains(result.Fields, field => field.FieldKey == IntakeFieldKeys.OfficialLetterDate && field.Value == "2026-03-10");
        }
        finally
        {
            if (File.Exists(pdfPath))
            {
                File.Delete(pdfPath);
            }
        }
    }

    [Fact]
    public async Task ProcessAsync_extracts_structured_fields_from_synthetic_scanned_pdf_when_python_worker_is_available()
    {
        using var harness = OcrTestHarness.Create();

        var pdfPath = await harness.CreateSyntheticScannedPdfAsync(
        [
            "GUARANTEE NUMBER BG-2026-4444",
            "REFERENCE EXT-ABC-999",
            "DATE 2026-03-10"
        ]);

        try
        {
            var result = await harness.Service.ProcessAsync(
                CreateRequest(
                    pdfPath,
                    "synthetic-scan.pdf",
                    IntakeScenarioKeys.ExtensionConfirmation,
                    GuaranteeDocumentFormKeys.BankLetterGeneric,
                    GuaranteeDocumentBankProfileKeys.Generic,
                    GuaranteeDocumentFormStructuralClassKeys.AmendmentLetter,
                    "Generic Bank",
                    "EXT"));

            Assert.True(result.Succeeded);
            Assert.Equal("wave2-scan-route", result.PipelineVersion);
            Assert.DoesNotContain("paddleocr-run-failed", result.Warnings);
            Assert.Contains(result.Fields, field => field.FieldKey == IntakeFieldKeys.GuaranteeNumber && field.Value == "BG-2026-4444");
            Assert.Contains(result.Fields, field => field.FieldKey == IntakeFieldKeys.BankReference && field.Value == "EXT-ABC-999");
            Assert.Contains(result.Fields, field => field.FieldKey == IntakeFieldKeys.OfficialLetterDate && field.Value == "2026-03-10");
        }
        finally
        {
            if (File.Exists(pdfPath))
            {
                File.Delete(pdfPath);
            }
        }
    }

    [Fact]
    public async Task ProcessAsync_falls_back_to_pdfium_when_pymupdf_rasterization_fails()
    {
        using var harness = OcrTestHarness.Create();

        var workerScriptPath = Path.Combine(Path.GetTempPath(), $"bg-ocr-pdfium-worker-{Guid.NewGuid():N}.py");
        var pdfPath = await harness.CreateSyntheticScannedPdfAsync(
        [
            "GUARANTEE NUMBER BG-2026-5555",
            "REFERENCE EXT-PDFIUM-555",
            "DATE 2026-03-10"
        ]);

        try
        {
            await File.WriteAllTextAsync(
                workerScriptPath,
                BuildPdfiumFallbackWorkerScript(harness.WorkerScriptPath),
                new UTF8Encoding(false));

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [$"{LocalOcrOptions.SectionName}:Enabled"] = "true",
                        [$"{LocalOcrOptions.SectionName}:PythonExecutablePath"] = harness.PythonExecutablePath,
                        [$"{LocalOcrOptions.SectionName}:WorkerScriptPath"] = workerScriptPath,
                        [$"{LocalOcrOptions.SectionName}:TimeoutSeconds"] = "180"
                    })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddIntegrations(configuration);

            using var serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetRequiredService<BG.Application.Contracts.Services.IOcrDocumentProcessingService>();

            var result = await service.ProcessAsync(
                CreateRequest(
                    pdfPath,
                    "synthetic-pdfium-scan.pdf",
                    IntakeScenarioKeys.ExtensionConfirmation,
                    GuaranteeDocumentFormKeys.BankLetterGeneric,
                    GuaranteeDocumentBankProfileKeys.Generic,
                    GuaranteeDocumentFormStructuralClassKeys.AmendmentLetter,
                    "Generic Bank",
                    "EXT"));

            Assert.True(result.Succeeded);
            Assert.Equal("wave2-scan-route", result.PipelineVersion);
            Assert.Contains("pdfium-raster-fallback", result.Warnings);
            Assert.DoesNotContain("pdfium-raster-failed", result.Warnings);
            Assert.Contains(result.Fields, field => field.FieldKey == IntakeFieldKeys.GuaranteeNumber && field.Value == "BG-2026-5555");
            Assert.Contains(result.Fields, field => field.FieldKey == IntakeFieldKeys.BankReference && field.Value == "EXT-PDFIUM-555");
        }
        finally
        {
            if (File.Exists(workerScriptPath))
            {
                File.Delete(workerScriptPath);
            }

            if (File.Exists(pdfPath))
            {
                File.Delete(pdfPath);
            }
        }
    }

    [Fact]
    public async Task ProcessAsync_handles_worker_stdout_noise_and_reads_json_payload()
    {
        using var harness = OcrTestHarness.Create();

        var workerScriptPath = Path.Combine(Path.GetTempPath(), $"bg-ocr-noisy-worker-{Guid.NewGuid():N}.py");
        var pdfPath = harness.CreateSimpleTextPdf("placeholder");

        try
        {
            await File.WriteAllTextAsync(
                workerScriptPath,
                """
                import json
                print("noise-before-json")
                print(json.dumps({
                    "succeeded": True,
                    "processorName": "bg-python-ocr",
                    "pipelineVersion": "noise-test",
                    "fields": [],
                    "warnings": [],
                    "errorCode": None,
                    "errorMessage": None
                }, ensure_ascii=False))
                """,
                new UTF8Encoding(false));

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [$"{LocalOcrOptions.SectionName}:Enabled"] = "true",
                        [$"{LocalOcrOptions.SectionName}:PythonExecutablePath"] = harness.PythonExecutablePath,
                        [$"{LocalOcrOptions.SectionName}:WorkerScriptPath"] = workerScriptPath,
                        [$"{LocalOcrOptions.SectionName}:TimeoutSeconds"] = "30"
                    })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddIntegrations(configuration);

            using var serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetRequiredService<BG.Application.Contracts.Services.IOcrDocumentProcessingService>();

            var result = await service.ProcessAsync(
                CreateRequest(
                    pdfPath,
                    "noise-test.pdf",
                    IntakeScenarioKeys.ExtensionConfirmation,
                    GuaranteeDocumentFormKeys.BankLetterGeneric,
                    GuaranteeDocumentBankProfileKeys.Generic,
                    GuaranteeDocumentFormStructuralClassKeys.AmendmentLetter,
                    "Generic Bank",
                    "EXT"));

            Assert.True(result.Succeeded);
            Assert.Equal("noise-test", result.PipelineVersion);
        }
        finally
        {
            if (File.Exists(workerScriptPath))
            {
                File.Delete(workerScriptPath);
            }

            if (File.Exists(pdfPath))
            {
                File.Delete(pdfPath);
            }
        }
    }

    [Fact]
    public async Task ProcessAsync_rejects_files_that_exceed_the_configured_size_limit()
    {
        using var harness = OcrTestHarness.Create();

        var oversizedPdfPath = Path.Combine(Path.GetTempPath(), $"bg-ocr-large-{Guid.NewGuid():N}.pdf");

        try
        {
            await File.WriteAllBytesAsync(oversizedPdfPath, new byte[2048]);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [$"{LocalOcrOptions.SectionName}:Enabled"] = "true",
                        [$"{LocalOcrOptions.SectionName}:PythonExecutablePath"] = harness.PythonExecutablePath,
                        [$"{LocalOcrOptions.SectionName}:WorkerScriptPath"] = harness.WorkerScriptPath,
                        [$"{LocalOcrOptions.SectionName}:TimeoutSeconds"] = "30",
                        [$"{LocalOcrOptions.SectionName}:MaxFileSizeBytes"] = "1024",
                        [$"{LocalOcrOptions.SectionName}:QueueCapacity"] = "2"
                    })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddIntegrations(configuration);

            using var serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetRequiredService<BG.Application.Contracts.Services.IOcrDocumentProcessingService>();

            var result = await service.ProcessAsync(
                CreateRequest(
                    oversizedPdfPath,
                    "oversized.pdf",
                    IntakeScenarioKeys.ExtensionConfirmation,
                    GuaranteeDocumentFormKeys.BankLetterGeneric,
                    GuaranteeDocumentBankProfileKeys.Generic,
                    GuaranteeDocumentFormStructuralClassKeys.AmendmentLetter,
                    "Generic Bank",
                    "EXT"));

            Assert.False(result.Succeeded);
            Assert.Equal("ocr.file_too_large", result.ErrorCode);
        }
        finally
        {
            if (File.Exists(oversizedPdfPath))
            {
                File.Delete(oversizedPdfPath);
            }
        }
    }

    [Theory]
    [MemberData(nameof(GetWave1ManifestPages))]
    public async Task ProcessAsync_processes_wave1_subset_pdf_pages(Wave1ManifestPage page)
    {
        using var harness = OcrTestHarness.Create();
        var pdfPath = harness.ResolveWave1Path(page.File);
        var result = await harness.Service.ProcessAsync(
            CreateRequest(
                pdfPath,
                page.File,
                page.ScenarioKey,
                page.DocumentFormKey,
                page.BankProfileKey,
                page.StructuralClassKey,
                page.CanonicalBankName,
                page.ReferencePrefix,
                [page.Page]));

        Assert.True(result.Succeeded);
        AssertMatchesExpectedTruth(
            result,
            LoadExpectedTruth(harness.ResolveWave1Path(page.ExpectedTruthFile)));
    }

    private static OcrDocumentProcessingRequest CreateRequest(
        string filePath,
        string originalFileName,
        string scenarioKey,
        string documentFormKey,
        string bankProfileKey,
        string structuralClassKey,
        string canonicalBankName,
        string referencePrefix,
        IReadOnlyList<int>? selectedPages = null)
    {
        return new OcrDocumentProcessingRequest(
            "token-1",
            filePath,
            originalFileName,
            scenarioKey,
            documentFormKey,
            bankProfileKey,
            structuralClassKey,
            canonicalBankName,
            referencePrefix,
            selectedPages);
    }

    private static string BuildPdfiumFallbackWorkerScript(string actualWorkerScriptPath)
    {
        var escapedPath = actualWorkerScriptPath.Replace("\\", "\\\\", StringComparison.Ordinal);

        return
            "import importlib.util\n" +
            "import sys\n" +
            $"worker_path = r'''{escapedPath}'''\n" +
            "spec = importlib.util.spec_from_file_location('bg_actual_worker', worker_path)\n" +
            "module = importlib.util.module_from_spec(spec)\n" +
            "assert spec is not None and spec.loader is not None\n" +
            "spec.loader.exec_module(module)\n" +
            "def force_pymupdf_failure(page, page_number):\n" +
            "    raise RuntimeError('forced pymupdf raster failure')\n" +
            "module.render_page_with_pymupdf = force_pymupdf_failure\n" +
            "raise SystemExit(module.main())\n";
    }

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BG.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to resolve repository root.");
    }

    private static OcrExpectedTruth LoadExpectedTruth(string path)
    {
        var json = File.ReadAllText(path, Encoding.UTF8);
        return JsonSerializer.Deserialize<OcrExpectedTruth>(json, TruthJsonOptions)
            ?? throw new InvalidOperationException($"Unable to parse OCR expected truth file '{path}'.");
    }

    private static void AssertMatchesExpectedTruth(
        OcrDocumentProcessingResult result,
        OcrExpectedTruth expected)
    {
        Assert.Equal(expected.PipelineVersion, result.PipelineVersion);

        foreach (var expectedField in expected.RequiredFields)
        {
            Assert.Contains(
                result.Fields,
                field =>
                    string.Equals(field.FieldKey, expectedField.FieldKey, StringComparison.Ordinal) &&
                    string.Equals(field.Value, expectedField.Value, StringComparison.Ordinal));
        }
    }

    private sealed record OcrExpectedTruth(
        string PipelineVersion,
        IReadOnlyList<OcrExpectedFieldTruth> RequiredFields);

    private sealed record OcrExpectedFieldTruth(
        string FieldKey,
        string Value);

    public static IEnumerable<object[]> GetWave1ManifestPages()
    {
        var manifest = LoadWave1Manifest();
        return manifest.Pages.Select(page => new object[] { page });
    }

    private static Wave1Manifest LoadWave1Manifest()
    {
        var manifestPath = Path.Combine(
            ResolveRepositoryRoot(),
            "tests",
            "BG.UnitTests",
            "TestData",
            "Ocr",
            "Wave1",
            "manifest.json");
        var json = File.ReadAllText(manifestPath, Encoding.UTF8);
        return JsonSerializer.Deserialize<Wave1Manifest>(json, TruthJsonOptions)
            ?? throw new InvalidOperationException($"Unable to parse OCR manifest '{manifestPath}'.");
    }

    public sealed record Wave1Manifest(IReadOnlyList<Wave1ManifestPage> Pages);

    public sealed record Wave1ManifestPage(
        string File,
        int Page,
        string ScenarioKey,
        string DocumentFormKey,
        string BankProfileKey,
        string StructuralClassKey,
        string Expectation)
    {
        public string ExpectedTruthFile => $"{Path.GetFileNameWithoutExtension(File)}-page{Page:00}.expected.json";

        public string CanonicalBankName => BankProfileKey switch
        {
            "riyad" => "Riyad Bank",
            "alrajhi" => "Al Rajhi Bank",
            "alinma" => "Alinma Bank",
            "bsf" => "Banque Saudi Fransi",
            _ => "Generic Bank"
        };

        public string ReferencePrefix => BankProfileKey switch
        {
            "riyad" => "RYD",
            "alrajhi" => "RJH",
            "alinma" => "ALN",
            "bsf" => "BSF",
            _ => string.Empty
        };
    }
}
