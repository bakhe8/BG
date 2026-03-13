using BG.Application.Models.Documents;

namespace BG.Application.Models.Approvals;

public sealed record ApprovalRequestAttachmentDto(
    Guid LinkId,
    Guid DocumentId,
    string FileName,
    string DocumentTypeResourceKey,
    DateTimeOffset LinkedAtUtc,
    string? LinkedByDisplayName,
    DateTimeOffset CapturedAtUtc,
    string? CapturedByDisplayName,
    string CaptureChannelResourceKey,
    string? SourceSystemName,
    string? SourceReference,
    GuaranteeDocumentFormSnapshotDto? DocumentForm);
