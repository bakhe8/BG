using System.Diagnostics;
using System.Text;
using BG.Application.Contracts.Services;
using BG.Integrations;
using BG.Integrations.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BG.UnitTests.Integrations;

internal sealed class OcrTestHarness : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly string _pythonExecutablePath;
    private readonly string _workerScriptPath;

    private OcrTestHarness(ServiceProvider serviceProvider, string pythonExecutablePath, string workerScriptPath)
    {
        _serviceProvider = serviceProvider;
        _pythonExecutablePath = pythonExecutablePath;
        _workerScriptPath = workerScriptPath;
    }

    public string PythonExecutablePath => _pythonExecutablePath;

    public string WorkerScriptPath => _workerScriptPath;

    public IOcrDocumentProcessingService Service => _serviceProvider.GetRequiredService<IOcrDocumentProcessingService>();

    public static OcrTestHarness Create()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var pythonExecutablePath = Path.Combine(repositoryRoot, ".venv-ocr312", "Scripts", "python.exe");
        var workerScriptPath = Path.Combine(repositoryRoot, "src", "BG.Integrations", "OcrWorker", "ocr_worker.py");

        if (!File.Exists(pythonExecutablePath) || !File.Exists(workerScriptPath))
        {
            throw new InvalidOperationException(
                $"OCR test harness requires '{pythonExecutablePath}' and '{workerScriptPath}'. Create the local OCR environment before running the mandatory OCR test suite.");
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{LocalOcrOptions.SectionName}:Enabled"] = "true",
                    [$"{LocalOcrOptions.SectionName}:PythonExecutablePath"] = pythonExecutablePath,
                    [$"{LocalOcrOptions.SectionName}:WorkerScriptPath"] = workerScriptPath,
                    [$"{LocalOcrOptions.SectionName}:TimeoutSeconds"] = "360"
                })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIntegrations(configuration);

        return new OcrTestHarness(services.BuildServiceProvider(), pythonExecutablePath, workerScriptPath);
    }

    public string CreateSimpleTextPdf(string text)
    {
        var pdfPath = Path.Combine(Path.GetTempPath(), $"bg-ocr-text-{Guid.NewGuid():N}.pdf");
        WriteSimpleTextPdf(pdfPath, text);
        return pdfPath;
    }

    public async Task<string> CreateSyntheticScannedPdfAsync(string[] lines, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_pythonExecutablePath))
        {
            throw new InvalidOperationException("OCR python executable is not available.");
        }

        var pdfPath = Path.Combine(Path.GetTempPath(), $"bg-ocr-scan-{Guid.NewGuid():N}.pdf");
        var scriptPath = Path.Combine(Path.GetTempPath(), $"bg-ocr-scan-gen-{Guid.NewGuid():N}.py");

        try
        {
            var script = BuildSyntheticScanScript(pdfPath, lines);
            await File.WriteAllTextAsync(scriptPath, script, new UTF8Encoding(false), cancellationToken);

            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonExecutablePath,
                Arguments = $"\"{scriptPath}\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var stdOut = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stdErr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0 || !File.Exists(pdfPath))
            {
                throw new InvalidOperationException(
                    $"Synthetic OCR scan PDF generation failed. stdout: {stdOut} stderr: {stdErr}");
            }

            return pdfPath;
        }
        finally
        {
            if (File.Exists(scriptPath))
            {
                File.Delete(scriptPath);
            }
        }
    }

    public string ResolveWave1Path(string fileName)
    {
        return Path.Combine(ResolveRepositoryRoot(), "tests", "BG.UnitTests", "TestData", "Ocr", "Wave1", fileName);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
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

        throw new InvalidOperationException("Unable to resolve the repository root for OCR tests.");
    }

    private static void WriteSimpleTextPdf(string path, string text)
    {
        var objects = new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>\nendobj\n",
            BuildStreamObject(text),
            "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n"
        };

        using var stream = File.Create(path);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.NewLine = "\n";

        writer.Write("%PDF-1.4\n");
        writer.Flush();

        var offsets = new List<long>();
        foreach (var pdfObject in objects)
        {
            writer.Flush();
            offsets.Add(stream.Position);
            writer.Write(pdfObject);
        }

        writer.Flush();
        var xrefOffset = stream.Position;
        writer.Write("xref\n");
        writer.Write($"0 {objects.Length + 1}\n");
        writer.Write("0000000000 65535 f \n");

        foreach (var offset in offsets)
        {
            writer.Write($"{offset:0000000000} 00000 n \n");
        }

        writer.Write("trailer\n");
        writer.Write($"<< /Size {objects.Length + 1} /Root 1 0 R >>\n");
        writer.Write("startxref\n");
        writer.Write($"{xrefOffset}\n");
        writer.Write("%%EOF");
    }

    private static string BuildStreamObject(string text)
    {
        var escapedText = text
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
        var streamContent = $"BT\n/F1 12 Tf\n72 720 Td\n({escapedText}) Tj\nET\n";
        var streamLength = Encoding.ASCII.GetByteCount(streamContent);
        return $"4 0 obj\n<< /Length {streamLength} >>\nstream\n{streamContent}endstream\nendobj\n";
    }

    private static string BuildSyntheticScanScript(string pdfPath, IReadOnlyList<string> lines)
    {
        var escapedPdfPath = pdfPath.Replace("\\", "\\\\", StringComparison.Ordinal);
        var pythonLines = string.Join(", ", lines.Select(line => $"r'''{line.Replace("'''", string.Empty, StringComparison.Ordinal)}'''"));

        return
            "import cv2, fitz, numpy as np, os, tempfile\n" +
            $"pdf_path = r'''{escapedPdfPath}'''\n" +
            $"lines = [{pythonLines}]\n" +
            "image = np.full((1600, 1200, 3), 255, dtype=np.uint8)\n" +
            "font = cv2.FONT_HERSHEY_SIMPLEX\n" +
            "y = 220\n" +
            "for line in lines:\n" +
            "    cv2.putText(image, line, (60, y), font, 1.6, (0, 0, 0), 3, cv2.LINE_AA)\n" +
            "    y += 170\n" +
            "image_path = os.path.join(tempfile.gettempdir(), 'bg-ocr-scan-temp.png')\n" +
            "cv2.imwrite(image_path, image)\n" +
            "doc = fitz.open()\n" +
            "page = doc.new_page(width=1200, height=1600)\n" +
            "page.insert_image(fitz.Rect(0, 0, 1200, 1600), filename=image_path)\n" +
            "doc.save(pdf_path)\n" +
            "doc.close()\n";
    }
}
