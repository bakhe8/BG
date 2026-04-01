namespace BG.Integrations.Options;

public sealed class LocalOcrOptions
{
    public const string SectionName = "Ocr";
    public const long DefaultMaxFileSizeBytes = 32L * 1024L * 1024L;

    public bool Enabled { get; set; }

    public string PythonExecutablePath { get; set; } = @"C:\Python314\python.exe";

    public string WorkerScriptPath { get; set; } = @"OcrWorker\ocr_worker.py";

    public int TimeoutSeconds { get; set; } = 90;

    public long MaxFileSizeBytes { get; set; } = DefaultMaxFileSizeBytes;

    public int QueueCapacity { get; set; } = 4;
}
