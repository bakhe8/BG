using BG.Application.Models.Approvals;
using BG.Application.Models.Dispatch;

namespace BG.Application.Contracts.Services;

public interface ILetterGenerationService
{
    Task<byte[]> GenerateLetterPdfAsync(
        DispatchLetterPreviewDto letter,
        IReadOnlyList<ApprovalPriorSignatureDto> signatures,
        CancellationToken cancellationToken = default);
}
