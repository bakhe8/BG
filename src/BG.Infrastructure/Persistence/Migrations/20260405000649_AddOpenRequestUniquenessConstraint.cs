using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenRequestUniquenessConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_guarantee_requests_open_owner_guarantee_type_unique",
                table: "guarantee_requests",
                columns: new[] { "RequestedByUserId", "GuaranteeId", "RequestType" },
                unique: true,
                filter: "\"Status\" <> 'Completed' AND \"Status\" <> 'Cancelled' AND \"Status\" <> 'Rejected'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_guarantee_requests_open_owner_guarantee_type_unique",
                table: "guarantee_requests");
        }
    }
}
