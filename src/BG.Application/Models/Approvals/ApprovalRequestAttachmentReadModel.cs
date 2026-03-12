using BG.Domain.Guarantees;

namespace BG.Application.Models.Approvals;

public sealed record ApprovalRequestAttachmentReadModel(
    Guid Id,
    Guid GuaranteeDocumentId,
    string FileName,
    GuaranteeDocumentType DocumentType,
    DateTimeOffset LinkedAtUtc,
    string? LinkedByDisplayName,
    DateTimeOffset CapturedAtUtc,
    string? CapturedByDisplayName,
    GuaranteeDocumentCaptureChannel CaptureChannel,
    string? SourceSystemName,
    string? SourceReference);
