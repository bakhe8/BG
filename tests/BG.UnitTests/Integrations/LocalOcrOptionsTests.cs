using BG.Integrations.Options;

namespace BG.UnitTests.Integrations;

public sealed class LocalOcrOptionsTests
{
    [Fact]
    public void Defaults_match_wave1_local_ocr_baseline()
    {
        var options = new LocalOcrOptions();

        Assert.False(options.Enabled);
        Assert.Equal(@"C:\Python314\python.exe", options.PythonExecutablePath);
        Assert.Equal(@"OcrWorker\ocr_worker.py", options.WorkerScriptPath);
        Assert.Equal(90, options.TimeoutSeconds);
    }
}
