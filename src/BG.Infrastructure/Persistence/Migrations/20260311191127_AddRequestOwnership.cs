using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RequestedByUserId",
                table: "guarantee_requests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_requests_RequestedByUserId_Status",
                table: "guarantee_requests",
                columns: new[] { "RequestedByUserId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_guarantee_requests_users_RequestedByUserId",
                table: "guarantee_requests",
                column: "RequestedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_guarantee_requests_users_RequestedByUserId",
                table: "guarantee_requests");

            migrationBuilder.DropIndex(
                name: "IX_guarantee_requests_RequestedByUserId_Status",
                table: "guarantee_requests");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "guarantee_requests");
        }
    }
}
