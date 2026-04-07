using BG.Infrastructure.Intake;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

internal sealed class OcrFeedbackRecordConfiguration : IEntityTypeConfiguration<OcrFeedbackRecord>
{
    public void Configure(EntityTypeBuilder<OcrFeedbackRecord> builder)
    {
        builder.ToTable("ocr_feedback_records");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.DocumentToken)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(record => record.ScenarioKey)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(record => record.FieldKey)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(record => record.DetectedBankName)
            .HasMaxLength(128);

        builder.Property(record => record.OriginalValue)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(record => record.CorrectedValue)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(record => record.Source)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(record => record.OriginalConfidencePercent)
            .IsRequired();

        builder.Property(record => record.RecordedAtUtc)
            .IsRequired();

        builder.HasIndex(record => record.RecordedAtUtc)
            .HasDatabaseName("IX_ocr_feedback_records_recorded_at");

        builder.HasIndex(record => new { record.ScenarioKey, record.FieldKey })
            .HasDatabaseName("IX_ocr_feedback_records_scenario_field");
    }
}
