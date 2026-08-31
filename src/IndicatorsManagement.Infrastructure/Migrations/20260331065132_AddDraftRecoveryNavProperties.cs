using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndicatorsManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftRecoveryNavProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_draft_recovery_IndicatorId",
                table: "draft_recovery",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_draft_recovery_ReportingPeriodId",
                table: "draft_recovery",
                column: "ReportingPeriodId");

            migrationBuilder.AddForeignKey(
                name: "FK_draft_recovery_indicators_IndicatorId",
                table: "draft_recovery",
                column: "IndicatorId",
                principalTable: "indicators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_draft_recovery_reporting_periods_ReportingPeriodId",
                table: "draft_recovery",
                column: "ReportingPeriodId",
                principalTable: "reporting_periods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_draft_recovery_indicators_IndicatorId",
                table: "draft_recovery");

            migrationBuilder.DropForeignKey(
                name: "FK_draft_recovery_reporting_periods_ReportingPeriodId",
                table: "draft_recovery");

            migrationBuilder.DropIndex(
                name: "IX_draft_recovery_IndicatorId",
                table: "draft_recovery");

            migrationBuilder.DropIndex(
                name: "IX_draft_recovery_ReportingPeriodId",
                table: "draft_recovery");
        }
    }
}
