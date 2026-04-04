using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDispatchTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailMessageId",
                table: "guarantee_correspondence",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailSendError",
                table: "guarantee_correspondence",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EmailSentAtUtc",
                table: "guarantee_correspondence",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailMessageId",
                table: "guarantee_correspondence");

            migrationBuilder.DropColumn(
                name: "EmailSendError",
                table: "guarantee_correspondence");

            migrationBuilder.DropColumn(
                name: "EmailSentAtUtc",
                table: "guarantee_correspondence");
        }
    }
}
