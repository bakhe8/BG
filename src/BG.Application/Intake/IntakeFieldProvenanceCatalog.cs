namespace BG.Application.Intake;

internal static class IntakeFieldProvenanceCatalog
{
    public static string GetResourceKey(IntakeFieldValueSource source)
    {
        return source switch
        {
            IntakeFieldValueSource.FilenamePattern => "IntakeFieldProvenance_FilenamePattern",
            IntakeFieldValueSource.DirectPdfText => "IntakeFieldProvenance_DirectPdfText",
            IntakeFieldValueSource.OcrFallback => "IntakeFieldProvenance_OcrFallback",
            IntakeFieldValueSource.ScenarioSample => "IntakeFieldProvenance_ScenarioSample",
            _ => "IntakeFieldProvenance_ScenarioSample"
        };
    }

    public static int GetPriority(IntakeFieldValueSource source)
    {
        return source switch
        {
            IntakeFieldValueSource.DirectPdfText => 400,
            IntakeFieldValueSource.OcrFallback => 300,
            IntakeFieldValueSource.FilenamePattern => 200,
            IntakeFieldValueSource.ScenarioSample => 100,
            _ => 0
        };
    }
}
