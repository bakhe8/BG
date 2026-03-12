using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntakeDocumentMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExtractionMethod",
                table: "guarantee_documents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntakeScenarioKey",
                table: "guarantee_documents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedDataJson",
                table: "guarantee_documents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtractionMethod",
                table: "guarantee_documents");

            migrationBuilder.DropColumn(
                name: "IntakeScenarioKey",
                table: "guarantee_documents");

            migrationBuilder.DropColumn(
                name: "VerifiedDataJson",
                table: "guarantee_documents");
        }
    }
}
