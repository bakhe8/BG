using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuaranteeDomainModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "guarantees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuaranteeNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BankName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BeneficiaryName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PrincipalName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CurrentAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SupersededByGuaranteeNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guarantees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "guarantee_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuaranteeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PageCount = table.Column<int>(type: "integer", nullable: false),
                    CapturedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guarantee_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_guarantee_documents_guarantees_GuaranteeId",
                        column: x => x.GuaranteeId,
                        principalTable: "guarantees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guarantee_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuaranteeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestType = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    Status = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    RequestedExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedToBankAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletionCorrespondenceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guarantee_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_guarantee_requests_guarantees_GuaranteeId",
                        column: x => x.GuaranteeId,
                        principalTable: "guarantees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guarantee_correspondence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuaranteeId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuaranteeRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    Direction = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Kind = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LetterDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ScannedDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RegisteredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AppliedToGuaranteeAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guarantee_correspondence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_guarantee_correspondence_guarantee_documents_ScannedDocumen~",
                        column: x => x.ScannedDocumentId,
                        principalTable: "guarantee_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_guarantee_correspondence_guarantee_requests_GuaranteeReques~",
                        column: x => x.GuaranteeRequestId,
                        principalTable: "guarantee_requests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_guarantee_correspondence_guarantees_GuaranteeId",
                        column: x => x.GuaranteeId,
                        principalTable: "guarantees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guarantee_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuaranteeId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuaranteeRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuaranteeCorrespondenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Summary = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PreviousAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    NewAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PreviousExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NewExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PreviousStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guarantee_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_guarantee_events_guarantee_correspondence_GuaranteeCorrespo~",
                        column: x => x.GuaranteeCorrespondenceId,
                        principalTable: "guarantee_correspondence",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_guarantee_events_guarantee_requests_GuaranteeRequestId",
                        column: x => x.GuaranteeRequestId,
                        principalTable: "guarantee_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_guarantee_events_guarantees_GuaranteeId",
                        column: x => x.GuaranteeId,
                        principalTable: "guarantees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_correspondence_GuaranteeId_LetterDate",
                table: "guarantee_correspondence",
                columns: new[] { "GuaranteeId", "LetterDate" });

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_correspondence_GuaranteeRequestId",
                table: "guarantee_correspondence",
                column: "GuaranteeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_correspondence_ScannedDocumentId",
                table: "guarantee_correspondence",
                column: "ScannedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_documents_GuaranteeId_CapturedAtUtc",
                table: "guarantee_documents",
                columns: new[] { "GuaranteeId", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_events_GuaranteeCorrespondenceId",
                table: "guarantee_events",
                column: "GuaranteeCorrespondenceId");

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_events_GuaranteeId_OccurredAtUtc",
                table: "guarantee_events",
                columns: new[] { "GuaranteeId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_events_GuaranteeRequestId",
                table: "guarantee_events",
                column: "GuaranteeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_requests_GuaranteeId_Status",
                table: "guarantee_requests",
                columns: new[] { "GuaranteeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_guarantees_GuaranteeNumber",
                table: "guarantees",
                column: "GuaranteeNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guarantee_events");

            migrationBuilder.DropTable(
                name: "guarantee_correspondence");

            migrationBuilder.DropTable(
                name: "guarantee_documents");

            migrationBuilder.DropTable(
                name: "guarantee_requests");

            migrationBuilder.DropTable(
                name: "guarantees");
        }
    }
}
