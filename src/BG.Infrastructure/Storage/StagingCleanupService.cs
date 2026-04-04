using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BG.Infrastructure.Storage;

internal sealed class StagingCleanupService : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan StagingFileMaxAge = TimeSpan.FromHours(24);

    private readonly IConfiguration _configuration;
    private readonly ILogger<StagingCleanupService> _logger;

    public StagingCleanupService(
        IConfiguration configuration,
        ILogger<StagingCleanupService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(RunInterval, stoppingToken);
                CleanupStagingFiles();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Staging file cleanup encountered an unexpected error.");
            }
        }
    }

    private void CleanupStagingFiles()
    {
        var rootPath = _configuration["Storage:DocumentsRoot"];
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return;
        }

        var stagingDirectory = Path.Combine(rootPath, "intake", "staging");
        if (!Directory.Exists(stagingDirectory))
        {
            return;
        }

        var cutoff = DateTime.UtcNow - StagingFileMaxAge;
        var deleted = 0;
        var errors = 0;

        foreach (var file in Directory.EnumerateFiles(stagingDirectory))
        {
            try
            {
                var lastWrite = File.GetLastWriteTimeUtc(file);
                if (lastWrite < cutoff)
                {
                    File.Delete(file);
                    deleted++;
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Could not delete orphaned staging file {FilePath}.", file);
                errors++;
            }
        }

        if (deleted > 0 || errors > 0)
        {
            _logger.LogInformation(
                "Staging cleanup completed: {Deleted} file(s) deleted, {Errors} error(s).",
                deleted,
                errors);
        }
    }
}
