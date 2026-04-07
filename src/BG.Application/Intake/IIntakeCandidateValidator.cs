namespace BG.Application.Intake;

internal interface IIntakeCandidateValidator
{
    IReadOnlyList<IntakeExtractionFieldCandidate> Validate(
        IEnumerable<IntakeExtractionFieldCandidate> candidates);
}
