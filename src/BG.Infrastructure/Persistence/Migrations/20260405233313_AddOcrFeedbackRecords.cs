using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOcrFeedbackRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ocr_feedback_records",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ScenarioKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DetectedBankName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    OriginalValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CorrectedValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OriginalConfidencePercent = table.Column<int>(type: "integer", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ocr_feedback_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ocr_feedback_records_recorded_at",
                table: "ocr_feedback_records",
                column: "RecordedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ocr_feedback_records_scenario_field",
                table: "ocr_feedback_records",
                columns: new[] { "ScenarioKey", "FieldKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ocr_feedback_records");
        }
    }
}
