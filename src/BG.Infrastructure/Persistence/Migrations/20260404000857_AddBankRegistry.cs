using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBankRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_banks_ShortCode",
                table: "banks",
                column: "ShortCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "banks");
        }
    }
}
