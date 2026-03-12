using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalLedgerPolicyContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally left empty. The original scaffold duplicated
            // workflow-governance columns already introduced by the previous migration.
            // A follow-up corrective migration adds the intended ledger columns.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op because Up is intentionally empty.
        }
    }
}
