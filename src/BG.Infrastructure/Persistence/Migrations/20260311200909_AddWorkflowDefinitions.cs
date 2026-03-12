using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "request_workflow_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    RequestType = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    GuaranteeCategory = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    GuaranteeCategoryResourceKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TitleResourceKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SummaryResourceKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_workflow_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "request_workflow_stages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    TitleResourceKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SummaryResourceKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CustomTitle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CustomSummary = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RequiresLetterSignature = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_workflow_stages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_request_workflow_stages_request_workflow_definitions_Workfl~",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "request_workflow_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_request_workflow_stages_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_request_workflow_definitions_Key",
                table: "request_workflow_definitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_workflow_stages_RoleId",
                table: "request_workflow_stages",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_request_workflow_stages_WorkflowDefinitionId_Sequence",
                table: "request_workflow_stages",
                columns: new[] { "WorkflowDefinitionId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "request_workflow_stages");

            migrationBuilder.DropTable(
                name: "request_workflow_definitions");
        }
    }
}
