using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndicatorsManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ResetSeedData_AddFundNetworkTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clean up old seed data so DatabaseSeeder can re-seed with the
            // correct 15 entities and 120 indicators from the official guide.
            // Order matters due to foreign key constraints.
            migrationBuilder.Sql("DELETE FROM indicator_entry_dimensions;");
            migrationBuilder.Sql("DELETE FROM attachments;");
            migrationBuilder.Sql("DELETE FROM version_history;");
            migrationBuilder.Sql("DELETE FROM publication_history;");
            migrationBuilder.Sql("DELETE FROM reopen_requests;");
            migrationBuilder.Sql("DELETE FROM indicator_entries;");
            migrationBuilder.Sql("DELETE FROM submission_obligations;");
            migrationBuilder.Sql("DELETE FROM indicator_assignments;");
            migrationBuilder.Sql("DELETE FROM target_values;");
            migrationBuilder.Sql("DELETE FROM validation_rules;");
            migrationBuilder.Sql("DELETE FROM dimension_values;");
            migrationBuilder.Sql("DELETE FROM dimensions;");
            migrationBuilder.Sql("DELETE FROM indicators;");
            migrationBuilder.Sql("DELETE FROM draft_recovery;");
            migrationBuilder.Sql("DELETE FROM entities;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
