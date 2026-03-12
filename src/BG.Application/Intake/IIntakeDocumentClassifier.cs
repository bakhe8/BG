using BG.Application.Models.Intake;

namespace BG.Application.Intake;

internal interface IIntakeDocumentClassifier
{
    IntakeDocumentClassificationResult Classify(StagedIntakeDocumentDto stagedDocument);
}
