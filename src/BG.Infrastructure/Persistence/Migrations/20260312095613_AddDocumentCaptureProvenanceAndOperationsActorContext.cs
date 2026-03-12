using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentCaptureProvenanceAndOperationsActorContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GuaranteeDocumentId",
                table: "guarantee_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CaptureChannel",
                table: "guarantee_documents",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CapturedByDisplayName",
                table: "guarantee_documents",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CapturedByUserId",
                table: "guarantee_documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceReference",
                table: "guarantee_documents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceSystemName",
                table: "guarantee_documents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_events_GuaranteeDocumentId",
                table: "guarantee_events",
                column: "GuaranteeDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_documents_CapturedByUserId",
                table: "guarantee_documents",
                column: "CapturedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_guarantee_documents_users_CapturedByUserId",
                table: "guarantee_documents",
                column: "CapturedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_guarantee_events_guarantee_documents_GuaranteeDocumentId",
                table: "guarantee_events",
                column: "GuaranteeDocumentId",
                principalTable: "guarantee_documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_guarantee_documents_users_CapturedByUserId",
                table: "guarantee_documents");

            migrationBuilder.DropForeignKey(
                name: "FK_guarantee_events_guarantee_documents_GuaranteeDocumentId",
                table: "guarantee_events");

            migrationBuilder.DropIndex(
                name: "IX_guarantee_events_GuaranteeDocumentId",
                table: "guarantee_events");

            migrationBuilder.DropIndex(
                name: "IX_guarantee_documents_CapturedByUserId",
                table: "guarantee_documents");

            migrationBuilder.DropColumn(
                name: "GuaranteeDocumentId",
                table: "guarantee_events");

            migrationBuilder.DropColumn(
                name: "CaptureChannel",
                table: "guarantee_documents");

            migrationBuilder.DropColumn(
                name: "CapturedByDisplayName",
                table: "guarantee_documents");

            migrationBuilder.DropColumn(
                name: "CapturedByUserId",
                table: "guarantee_documents");

            migrationBuilder.DropColumn(
                name: "SourceReference",
                table: "guarantee_documents");

            migrationBuilder.DropColumn(
                name: "SourceSystemName",
                table: "guarantee_documents");
        }
    }
}
