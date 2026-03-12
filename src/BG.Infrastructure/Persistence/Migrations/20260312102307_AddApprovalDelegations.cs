using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalDelegations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActedOnBehalfOfUserId",
                table: "request_approval_stages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovalDelegationId",
                table: "request_approval_stages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "approval_delegations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegatorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_delegations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_approval_delegations_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_approval_delegations_users_DelegateUserId",
                        column: x => x.DelegateUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_approval_delegations_users_DelegatorUserId",
                        column: x => x.DelegatorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_request_approval_stages_ActedOnBehalfOfUserId",
                table: "request_approval_stages",
                column: "ActedOnBehalfOfUserId");

            migrationBuilder.CreateIndex(
                name: "IX_request_approval_stages_ApprovalDelegationId",
                table: "request_approval_stages",
                column: "ApprovalDelegationId");

            migrationBuilder.CreateIndex(
                name: "IX_approval_delegations_DelegateUserId_RoleId_StartsAtUtc_Ends~",
                table: "approval_delegations",
                columns: new[] { "DelegateUserId", "RoleId", "StartsAtUtc", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_approval_delegations_DelegatorUserId",
                table: "approval_delegations",
                column: "DelegatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_approval_delegations_RoleId",
                table: "approval_delegations",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_request_approval_stages_approval_delegations_ApprovalDelega~",
                table: "request_approval_stages",
                column: "ApprovalDelegationId",
                principalTable: "approval_delegations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_request_approval_stages_users_ActedOnBehalfOfUserId",
                table: "request_approval_stages",
                column: "ActedOnBehalfOfUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_request_approval_stages_approval_delegations_ApprovalDelega~",
                table: "request_approval_stages");

            migrationBuilder.DropForeignKey(
                name: "FK_request_approval_stages_users_ActedOnBehalfOfUserId",
                table: "request_approval_stages");

            migrationBuilder.DropTable(
                name: "approval_delegations");

            migrationBuilder.DropIndex(
                name: "IX_request_approval_stages_ActedOnBehalfOfUserId",
                table: "request_approval_stages");

            migrationBuilder.DropIndex(
                name: "IX_request_approval_stages_ApprovalDelegationId",
                table: "request_approval_stages");

            migrationBuilder.DropColumn(
                name: "ActedOnBehalfOfUserId",
                table: "request_approval_stages");

            migrationBuilder.DropColumn(
                name: "ApprovalDelegationId",
                table: "request_approval_stages");
        }
    }
}
