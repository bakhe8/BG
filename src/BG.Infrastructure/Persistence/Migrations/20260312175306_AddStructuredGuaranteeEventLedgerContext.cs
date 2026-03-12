using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredGuaranteeEventLedgerContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalExecutionMode",
                table: "guarantee_events",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalPolicyResourceKey",
                table: "guarantee_events",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalResponsibleSignerDisplayName",
                table: "guarantee_events",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStageLabel",
                table: "guarantee_events",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchMethodResourceKey",
                table: "guarantee_events",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchPolicyResourceKey",
                table: "guarantee_events",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchStageResourceKey",
                table: "guarantee_events",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationsLaneResourceKey",
                table: "guarantee_events",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationsMatchConfidenceResourceKey",
                table: "guarantee_events",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OperationsMatchScore",
                table: "guarantee_events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationsPolicyResourceKey",
                table: "guarantee_events",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationsScenarioTitleResourceKey",
                table: "guarantee_events",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalExecutionMode",
                table: "guarantee_events");

            migrationBuilder.DropColumn(
                name: "ApprovalPolicyResourceKey",
                table: "guarantee_events");

            migrationBuilder.DropColumn(
                name: "ApprovalResponsibleSignerDisplayName",
                table: "guarantee_events");

            migrationBuilder.DropColumn(
                name: "ApprovalStageLabel",
                table: "guarantee_events");

            migrationBuilder.DropColumn(
                name: "DispatchMethodResourceKey",
                table: "guarantee_events");

            migrationBuilder.DropColumn(
                name: "DispatchPolicyResourceKey",
                table: "guarantee_events");

            migrationBuilder.DropColumn(
                name: "DispatchStageResourceKey",
                table: "guarantee_events");

            migrationBuilder.DropColumn(
                name: "OperationsLaneResourceKey",
                table: "guarantee_events");

            migrationBuilder.DropColumn(
                name: "OperationsMatchConfidenceResourceKey",
                table: "guarantee_events");

            migrationBuilder.DropColumn(
                name: "OperationsMatchScore",
                table: "guarantee_events");

            migrationBuilder.DropColumn(
                name: "OperationsPolicyResourceKey",
                table: "guarantee_events");

            migrationBuilder.DropColumn(
                name: "OperationsScenarioTitleResourceKey",
                table: "guarantee_events");
        }
    }
}
