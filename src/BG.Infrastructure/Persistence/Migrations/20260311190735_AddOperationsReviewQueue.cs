using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationsReviewQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "operations_review_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuaranteeId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuaranteeNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GuaranteeDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuaranteeCorrespondenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScenarioKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RoutedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RoutedToLaneKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operations_review_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_operations_review_items_guarantee_correspondence_GuaranteeC~",
                        column: x => x.GuaranteeCorrespondenceId,
                        principalTable: "guarantee_correspondence",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_operations_review_items_guarantee_documents_GuaranteeDocume~",
                        column: x => x.GuaranteeDocumentId,
                        principalTable: "guarantee_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_operations_review_items_guarantees_GuaranteeId",
                        column: x => x.GuaranteeId,
                        principalTable: "guarantees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_operations_review_items_GuaranteeCorrespondenceId",
                table: "operations_review_items",
                column: "GuaranteeCorrespondenceId");

            migrationBuilder.CreateIndex(
                name: "IX_operations_review_items_GuaranteeDocumentId",
                table: "operations_review_items",
                column: "GuaranteeDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_operations_review_items_GuaranteeId_Status",
                table: "operations_review_items",
                columns: new[] { "GuaranteeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_operations_review_items_Status_CreatedAtUtc",
                table: "operations_review_items",
                columns: new[] { "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operations_review_items");
        }
    }
}
