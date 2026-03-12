using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuaranteeLedgerActorFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActorDisplayName",
                table: "guarantee_events",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActorUserId",
                table: "guarantee_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_guarantee_events_ActorUserId",
                table: "guarantee_events",
                column: "ActorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_guarantee_events_users_ActorUserId",
                table: "guarantee_events",
                column: "ActorUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_guarantee_events_users_ActorUserId",
                table: "guarantee_events");

            migrationBuilder.DropIndex(
                name: "IX_guarantee_events_ActorUserId",
                table: "guarantee_events");

            migrationBuilder.DropColumn(
                name: "ActorDisplayName",
                table: "guarantee_events");

            migrationBuilder.DropColumn(
                name: "ActorUserId",
                table: "guarantee_events");
        }
    }
}
