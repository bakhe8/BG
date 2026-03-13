using BG.Domain.Guarantees;

namespace BG.Application.Models.Intake;

public sealed record IntakeSubmissionCommand(
    Guid IntakeActorUserId,
    string ScenarioKey,
    string StagedDocumentToken,
    string DocumentFormKey,
    string ExtractionRouteResourceKey,
    int PageCount,
    GuaranteeDocumentCaptureChannel CaptureChannel,
    string? SourceSystemName,
    string? SourceReference,
    string? GuaranteeNumber,
    string? BankName,
    string? BeneficiaryName,
    string? PrincipalName,
    GuaranteeCategory? GuaranteeCategory,
    string? Amount,
    string? CurrencyCode,
    string? IssueDate,
    string? ExpiryDate,
    string? OfficialLetterDate,
    string? NewExpiryDate,
    string? BankReference,
    string? StatusStatement,
    string? AttachmentNote);
