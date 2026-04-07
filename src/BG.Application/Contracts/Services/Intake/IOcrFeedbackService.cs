using BG.Application.Models.Intake;

namespace BG.Application.Contracts.Services;

public interface IOcrFeedbackService
{
    Task RecordAsync(
        IReadOnlyList<OcrFieldFeedbackEntryDto> entries,
        CancellationToken cancellationToken = default);
}
