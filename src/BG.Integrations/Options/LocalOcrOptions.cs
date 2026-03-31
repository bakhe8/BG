namespace BG.Integrations.Options;

public sealed class LocalOcrOptions
{
    public const string SectionName = "Ocr";

    public bool Enabled { get; set; }

    public string PythonExecutablePath { get; set; } = @"C:\Python314\python.exe";

    public string WorkerScriptPath { get; set; } = @"OcrWorker\ocr_worker.py";

    public int TimeoutSeconds { get; set; } = 90;
}
