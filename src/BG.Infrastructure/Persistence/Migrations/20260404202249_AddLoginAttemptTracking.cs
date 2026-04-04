using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginAttemptTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredCulture",
                table: "users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredTheme",
                table: "users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "banks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CanonicalName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ShortCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    OfficialEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsEmailDispatchEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SupportedDispatchChannels = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_banks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginAttemptRecords",
                columns: table => new
                {
                    TrackingKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FailureCount = table.Column<int>(type: "integer", nullable: false),
                    WindowExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LockedUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttemptRecords", x => x.TrackingKey);
                });

            migrationBuilder.CreateIndex(
                name: "IX_banks_ShortCode",
                table: "banks",
                column: "ShortCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttemptRecords_WindowExpiresAtUtc",
                table: "LoginAttemptRecords",
                column: "WindowExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "banks");

            migrationBuilder.DropTable(
                name: "LoginAttemptRecords");

            migrationBuilder.DropColumn(
                name: "PreferredCulture",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PreferredTheme",
                table: "users");
        }
    }
}
