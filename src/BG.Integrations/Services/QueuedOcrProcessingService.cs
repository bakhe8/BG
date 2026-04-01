using System.Threading.Channels;
using System.Threading;
using BG.Application.Contracts.Services;
using BG.Application.Models.Intake;
using BG.Integrations.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BG.Integrations.Services;

internal sealed class QueuedOcrProcessingService :
    IOcrDocumentProcessingService,
    IHostedService,
    IDisposable,
    IAsyncDisposable
{
    private readonly Channel<OcrQueueItem> _queue;
    private readonly ILocalOcrWorkerRunner _workerRunner;
    private readonly LocalOcrOptions _options;
    private readonly ILogger<QueuedOcrProcessingService> _logger;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly object _processorGate = new();
    private Task? _processorTask;
    private int _disposeState;

    public QueuedOcrProcessingService(
        IOptions<LocalOcrOptions> options,
        ILocalOcrWorkerRunner workerRunner,
        ILogger<QueuedOcrProcessingService> logger)
    {
        _options = options.Value;
        _workerRunner = workerRunner;
        _logger = logger;

        var queueCapacity = Math.Clamp(_options.QueueCapacity, 1, 32);
        _queue = Channel.CreateBounded<OcrQueueItem>(
            new BoundedChannelOptions(queueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return Task.CompletedTask;
        }

        EnsureProcessorStarted();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _queue.Writer.TryComplete();
        _shutdown.Cancel();

        if (_processorTask is null)
        {
            return;
        }

        await Task.WhenAny(_processorTask, Task.Delay(Timeout.Infinite, cancellationToken));
    }

    public async Task<OcrDocumentProcessingResult> ProcessAsync(
        OcrDocumentProcessingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return await _workerRunner.ProcessAsync(request, cancellationToken);
        }

        EnsureProcessorStarted();

        var queueItem = new OcrQueueItem(request, cancellationToken);
        using var registration = cancellationToken.Register(
            static state =>
            {
                var item = (OcrQueueItem)state!;
                item.Completion.TrySetCanceled(item.CancellationToken);
            },
            queueItem);

        try
        {
            await _queue.Writer.WriteAsync(queueItem, cancellationToken);
        }
        catch (ChannelClosedException)
        {
            _logger.LogError("OCR queue is unavailable because the processor is shutting down.");
            return new OcrDocumentProcessingResult(
                false,
                "bg-python-ocr",
                "wave2-queued",
                [],
                [],
                "ocr.queue_unavailable",
                "The OCR queue is unavailable because the processor is shutting down.");
        }

        return await queueItem.Completion.Task.WaitAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            return;
        }

        _queue.Writer.TryComplete();
        _shutdown.Cancel();

        if (_processorTask is not null)
        {
            try
            {
                await _processorTask;
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation during shutdown.
            }
        }

        _shutdown.Dispose();
    }

    public void Dispose()
    {
        if (Volatile.Read(ref _disposeState) != 0)
        {
            return;
        }

        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private void EnsureProcessorStarted()
    {
        if (_processorTask is not null)
        {
            return;
        }

        lock (_processorGate)
        {
            _processorTask ??= Task.Run(RunProcessorAsync);
        }
    }

    private async Task RunProcessorAsync()
    {
        try
        {
            while (await _queue.Reader.WaitToReadAsync(_shutdown.Token))
            {
                while (_queue.Reader.TryRead(out var queueItem))
                {
                    if (queueItem.Completion.Task.IsCompleted || queueItem.CancellationToken.IsCancellationRequested)
                    {
                        queueItem.Completion.TrySetCanceled(queueItem.CancellationToken);
                        continue;
                    }

                    try
                    {
                        using var linkedCancellationTokenSource =
                            CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token, queueItem.CancellationToken);

                        var result = await _workerRunner.ProcessAsync(queueItem.Request, linkedCancellationTokenSource.Token);
                        queueItem.Completion.TrySetResult(result);
                    }
                    catch (OperationCanceledException) when (_shutdown.IsCancellationRequested || queueItem.CancellationToken.IsCancellationRequested)
                    {
                        queueItem.Completion.TrySetCanceled(queueItem.CancellationToken.IsCancellationRequested
                            ? queueItem.CancellationToken
                            : _shutdown.Token);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "Queued OCR processing failed unexpectedly.");
                        queueItem.Completion.TrySetResult(
                            new OcrDocumentProcessingResult(
                                false,
                                "bg-python-ocr",
                                "wave2-queued",
                                [],
                                [],
                                "ocr.queue_failed",
                                "The OCR queue failed to process the document."));
                    }
                }
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            // Expected during application shutdown.
        }
        finally
        {
            while (_queue.Reader.TryRead(out var pendingItem))
            {
                pendingItem.Completion.TrySetCanceled(_shutdown.Token);
            }
        }
    }

    private sealed class OcrQueueItem
    {
        public OcrQueueItem(
            OcrDocumentProcessingRequest request,
            CancellationToken cancellationToken)
        {
            Request = request;
            CancellationToken = cancellationToken;
        }

        public OcrDocumentProcessingRequest Request { get; }

        public CancellationToken CancellationToken { get; }

        public TaskCompletionSource<OcrDocumentProcessingResult> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
