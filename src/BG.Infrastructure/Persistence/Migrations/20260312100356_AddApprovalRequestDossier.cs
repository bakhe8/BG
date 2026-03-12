using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalRequestDossier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestChannel",
                table: "guarantee_requests",
                type: "character varying(48)",
                maxLength: 48,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "guarantee_request_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuaranteeRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuaranteeDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LinkedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedByDisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guarantee_request_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_guarantee_request_documents_guarantee_documents_GuaranteeDo~",
                        column: x => x.GuaranteeDocumentId,
                        principalTable: "guarantee_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_guarantee_request_documents_guarantee_requests_GuaranteeReq~",
                        column: x => x.GuaranteeRequestId,
                        principalTable: "guarantee_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_guarantee_request_documents_users_LinkedByUserId",
                        column: x => x.LinkedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_request_documents_GuaranteeDocumentId",
                table: "guarantee_request_documents",
                column: "GuaranteeDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_request_documents_GuaranteeRequestId_GuaranteeDoc~",
                table: "guarantee_request_documents",
                columns: new[] { "GuaranteeRequestId", "GuaranteeDocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_request_documents_LinkedByUserId",
                table: "guarantee_request_documents",
                column: "LinkedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guarantee_request_documents");

            migrationBuilder.DropColumn(
                name: "RequestChannel",
                table: "guarantee_requests");
        }
    }
}
