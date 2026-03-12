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
            IntakeFieldValueSource.FilenamePattern => 400,
            IntakeFieldValueSource.DirectPdfText => 300,
            IntakeFieldValueSource.OcrFallback => 200,
            IntakeFieldValueSource.ScenarioSample => 100,
            _ => 0
        };
    }
}
