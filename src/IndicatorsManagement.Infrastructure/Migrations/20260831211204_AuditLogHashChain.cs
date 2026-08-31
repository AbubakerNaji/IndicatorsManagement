using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndicatorsManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuditLogHashChain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreviousHash",
                table: "audit_logs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RowHash",
                table: "audit_logs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviousHash",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "RowHash",
                table: "audit_logs");
        }
    }
}
