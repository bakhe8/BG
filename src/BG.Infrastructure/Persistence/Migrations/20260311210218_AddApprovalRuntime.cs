using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "request_approval_processes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuaranteeRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastReturnedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastReturnedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastReturnedNote = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    LastRejectedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastRejectedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastRejectedNote = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_approval_processes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_request_approval_processes_guarantee_requests_GuaranteeRequ~",
                        column: x => x.GuaranteeRequestId,
                        principalTable: "guarantee_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_request_approval_processes_request_workflow_definitions_Wor~",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "request_workflow_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "request_approval_stages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    TitleResourceKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SummaryResourceKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TitleText = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SummaryText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RequiresLetterSignature = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ActedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecisionNote = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SignatureAppliedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_approval_stages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_request_approval_stages_request_approval_processes_Approval~",
                        column: x => x.ApprovalProcessId,
                        principalTable: "request_approval_processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_request_approval_stages_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_approval_stages_users_ActedByUserId",
                        column: x => x.ActedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_request_approval_processes_GuaranteeRequestId",
                table: "request_approval_processes",
                column: "GuaranteeRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_approval_processes_WorkflowDefinitionId",
                table: "request_approval_processes",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_request_approval_stages_ActedByUserId",
                table: "request_approval_stages",
                column: "ActedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_request_approval_stages_ApprovalProcessId_Sequence",
                table: "request_approval_stages",
                columns: new[] { "ApprovalProcessId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_approval_stages_RoleId",
                table: "request_approval_stages",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "request_approval_stages");

            migrationBuilder.DropTable(
                name: "request_approval_processes");
        }
    }
}
