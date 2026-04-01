using System.Globalization;
using System.Diagnostics;
using System.Text.Json;
using BG.Application.Models.Intake;
using BG.Integrations.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BG.Integrations.Services;

internal sealed class LocalPythonOcrProcessingService : ILocalOcrWorkerRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string WorkerPipelineVersion = "wave2-python-worker";
    private readonly LocalOcrOptions _options;
    private readonly ILogger<LocalPythonOcrProcessingService> _logger;

    public LocalPythonOcrProcessingService(
        IOptions<LocalOcrOptions> options,
        ILogger<LocalPythonOcrProcessingService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<OcrDocumentProcessingResult> ProcessAsync(
        OcrDocumentProcessingRequest request,
        CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["OriginalFileName"] = request.OriginalFileName,
            ["ScenarioKey"] = request.ScenarioKey,
            ["DocumentFormKey"] = request.DocumentFormKey,
            ["BankProfileKey"] = request.BankProfileKey
        });

        if (!_options.Enabled)
        {
            _logger.LogDebug("Local OCR processing is disabled.");
            return OcrDocumentProcessingResult.Disabled("bg-python-ocr");
        }

        if (string.IsNullOrWhiteSpace(request.FilePath) || !File.Exists(request.FilePath))
        {
            _logger.LogWarning("OCR input file was not found at {FilePath}.", request.FilePath);
            return new OcrDocumentProcessingResult(
                false,
                "bg-python-ocr",
                WorkerPipelineVersion,
                [],
                [],
                "ocr.file_not_found",
                "The staged OCR input file was not found.");
        }

        var maxFileSizeBytes = ResolveMaxFileSizeBytes();
        var inputFileInfo = new FileInfo(request.FilePath);
        if (inputFileInfo.Length > maxFileSizeBytes)
        {
            _logger.LogWarning(
                "OCR input file size {FileSizeBytes} bytes exceeded the configured maximum of {MaxFileSizeBytes} bytes.",
                inputFileInfo.Length,
                maxFileSizeBytes);

            return new OcrDocumentProcessingResult(
                false,
                "bg-python-ocr",
                WorkerPipelineVersion,
                [],
                [],
                "ocr.file_too_large",
                $"The OCR input file exceeded the configured size limit of {FormatFileSize(maxFileSizeBytes)}.");
        }

        var pythonExecutablePath = ResolveExecutablePath(_options.PythonExecutablePath);
        if (string.IsNullOrWhiteSpace(pythonExecutablePath) || !File.Exists(pythonExecutablePath))
        {
            _logger.LogWarning("Configured OCR Python executable was not found at {PythonExecutablePath}.", pythonExecutablePath);
            return new OcrDocumentProcessingResult(
                false,
                "bg-python-ocr",
                WorkerPipelineVersion,
                [],
                [],
                "ocr.python_not_found",
                "The configured Python executable was not found.");
        }

        var workerScriptPath = ResolveWorkerScriptPath(_options.WorkerScriptPath);
        if (!File.Exists(workerScriptPath))
        {
            _logger.LogWarning("Configured OCR worker script was not found at {WorkerScriptPath}.", workerScriptPath);
            return new OcrDocumentProcessingResult(
                false,
                "bg-python-ocr",
                WorkerPipelineVersion,
                [],
                [],
                "ocr.worker_not_found",
                "The configured OCR worker script was not found.");
        }

        var requestFilePath = Path.Combine(Path.GetTempPath(), $"bg-ocr-{Guid.NewGuid():N}.json");
        Process? process = null;

        try
        {
            _logger.LogInformation(
                "Starting OCR worker with Python executable {PythonExecutablePath} and worker script {WorkerScriptPath}.",
                pythonExecutablePath,
                workerScriptPath);

            await File.WriteAllTextAsync(
                requestFilePath,
                JsonSerializer.Serialize(request, JsonOptions),
                cancellationToken);

            var startInfo = new ProcessStartInfo
            {
                FileName = pythonExecutablePath,
                Arguments = $"\"{workerScriptPath}\" --request \"{requestFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.Environment["PADDLE_PDX_DISABLE_MODEL_SOURCE_CHECK"] = "True";
            startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
            startInfo.Environment["PYTHONUTF8"] = "1";
            startInfo.Environment["PYTHONWARNINGS"] = "ignore";
            startInfo.Environment["HF_HUB_DISABLE_PROGRESS_BARS"] = "1";

            process = new Process
            {
                StartInfo = startInfo
            };
            startInfo.Environment["BG_OCR_MAX_FILE_SIZE_BYTES"] = maxFileSizeBytes.ToString(CultureInfo.InvariantCulture);

            process.Start();

            var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var waitTask = process.WaitForExitAsync(cancellationToken);

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 300)), cancellationToken);
            var completedTask = await Task.WhenAny(waitTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                TryKill(process);
                _logger.LogError(
                    "OCR worker timed out after {TimeoutSeconds} seconds.",
                    Math.Clamp(_options.TimeoutSeconds, 5, 300));

                return new OcrDocumentProcessingResult(
                    false,
                    "bg-python-ocr",
                    WorkerPipelineVersion,
                    [],
                    [],
                    "ocr.timeout",
                    "The OCR worker timed out.");
            }

            await waitTask;

            var standardOutput = await standardOutputTask;
            var standardError = await standardErrorTask;

            if (process.ExitCode != 0)
            {
                _logger.LogError(
                    "OCR worker failed with exit code {ExitCode}. stderr: {StandardError}",
                    process.ExitCode,
                    TrimDiagnostic(standardError));

                return new OcrDocumentProcessingResult(
                    false,
                    "bg-python-ocr",
                    WorkerPipelineVersion,
                    [],
                    [],
                    "ocr.worker_failed",
                    string.IsNullOrWhiteSpace(standardError) ? "The OCR worker failed." : standardError.Trim());
            }

            var result = TryDeserializeWorkerResult(standardOutput);
            if (result is null)
            {
                _logger.LogError(
                    "OCR worker returned an invalid response. stdout: {StandardOutput} stderr: {StandardError}",
                    TrimDiagnostic(standardOutput),
                    TrimDiagnostic(standardError));

                return new OcrDocumentProcessingResult(
                    false,
                    "bg-python-ocr",
                    WorkerPipelineVersion,
                    [],
                    [],
                    "ocr.invalid_response",
                    "The OCR worker returned an invalid response.");
            }

            if (!result.Succeeded)
            {
                _logger.LogWarning(
                    "OCR worker returned a failure result with error code {ErrorCode}: {ErrorMessage}",
                    result.ErrorCode,
                    result.ErrorMessage);
                return result;
            }

            if (result.Warnings.Count > 0)
            {
                _logger.LogWarning(
                    "OCR worker completed with warnings: {Warnings}",
                    string.Join(", ", result.Warnings));
            }

            _logger.LogInformation(
                "OCR worker completed using pipeline {PipelineVersion} with {FieldCount} extracted fields.",
                result.PipelineVersion,
                result.Fields.Count);

            return result;
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "OCR worker process execution failed.");
            return new OcrDocumentProcessingResult(
                false,
                "bg-python-ocr",
                WorkerPipelineVersion,
                [],
                [],
                "ocr.process_exception",
                exception.Message);
        }
        finally
        {
            TryKill(process);
            if (File.Exists(requestFilePath))
            {
                File.Delete(requestFilePath);
            }
        }
    }

    private static string TrimDiagnostic(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        const int maxLength = 500;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength
            ? trimmed
            : $"{trimmed[..maxLength]}...";
    }

    private static string ResolveWorkerScriptPath(string configuredPath)
    {
        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);
    }

    private static string ResolveExecutablePath(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        var currentDirectoryPath = Path.GetFullPath(configuredPath, Directory.GetCurrentDirectory());
        if (File.Exists(currentDirectoryPath))
        {
            return currentDirectoryPath;
        }

        return Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
    }

    private long ResolveMaxFileSizeBytes()
    {
        return _options.MaxFileSizeBytes > 0
            ? _options.MaxFileSizeBytes
            : LocalOcrOptions.DefaultMaxFileSizeBytes;
    }

    private static string FormatFileSize(long sizeInBytes)
    {
        return sizeInBytes switch
        {
            >= 1024L * 1024L => $"{sizeInBytes / (1024d * 1024d):0.#} MB",
            >= 1024L => $"{sizeInBytes / 1024d:0.#} KB",
            _ => $"{sizeInBytes} bytes"
        };
    }

    private static void TryKill(Process? process)
    {
        try
        {
            if (process is not null && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Ignore cleanup failures from the worker process.
        }
    }

    private static OcrDocumentProcessingResult? TryDeserializeWorkerResult(string standardOutput)
    {
        if (string.IsNullOrWhiteSpace(standardOutput))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<OcrDocumentProcessingResult>(standardOutput, JsonOptions);
        }
        catch (JsonException)
        {
            var lines = standardOutput
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            for (var index = lines.Length - 1; index >= 0; index--)
            {
                var line = lines[index];
                if (!line.StartsWith('{'))
                {
                    continue;
                }

                try
                {
                    return JsonSerializer.Deserialize<OcrDocumentProcessingResult>(line, JsonOptions);
                }
                catch (JsonException)
                {
                    // Keep scanning backwards for the last JSON payload line.
                }
            }

            return null;
        }
    }
}
