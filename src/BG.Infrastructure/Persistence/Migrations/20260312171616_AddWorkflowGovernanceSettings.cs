using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowGovernanceSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DelegationAmountThreshold",
                table: "request_workflow_definitions",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalSignatureDelegationPolicy",
                table: "request_workflow_definitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Inherit");

            migrationBuilder.AddColumn<decimal>(
                name: "DelegationAmountThreshold",
                table: "request_approval_processes",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalSignatureDelegationPolicy",
                table: "request_approval_processes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Inherit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DelegationAmountThreshold",
                table: "request_workflow_definitions");

            migrationBuilder.DropColumn(
                name: "FinalSignatureDelegationPolicy",
                table: "request_workflow_definitions");

            migrationBuilder.DropColumn(
                name: "DelegationAmountThreshold",
                table: "request_approval_processes");

            migrationBuilder.DropColumn(
                name: "FinalSignatureDelegationPolicy",
                table: "request_approval_processes");
        }
    }
}
