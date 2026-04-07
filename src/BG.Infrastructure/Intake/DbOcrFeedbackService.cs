using BG.Application.Contracts.Services;
using BG.Application.Models.Intake;
using BG.Infrastructure.Persistence;

namespace BG.Infrastructure.Intake;

internal sealed class DbOcrFeedbackService : IOcrFeedbackService
{
    private readonly BgDbContext _dbContext;

    public DbOcrFeedbackService(BgDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RecordAsync(
        IReadOnlyList<OcrFieldFeedbackEntryDto> entries,
        CancellationToken cancellationToken = default)
    {
        if (entries.Count == 0)
            return;

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entries)
        {
            _dbContext.Set<OcrFeedbackRecord>().Add(new OcrFeedbackRecord
            {
                DocumentToken = entry.DocumentToken,
                ScenarioKey = entry.ScenarioKey,
                FieldKey = entry.FieldKey,
                DetectedBankName = entry.DetectedBankName,
                OriginalValue = entry.OriginalValue,
                CorrectedValue = entry.CorrectedValue,
                Source = entry.Source,
                OriginalConfidencePercent = entry.OriginalConfidencePercent,
                RecordedAtUtc = now
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
