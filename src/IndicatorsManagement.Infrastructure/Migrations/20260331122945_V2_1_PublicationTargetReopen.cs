using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndicatorsManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V2_1_PublicationTargetReopen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "publication_history",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndicatorEntryId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PerformedBy = table.Column<int>(type: "int", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_publication_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_publication_history_AspNetUsers_PerformedBy",
                        column: x => x.PerformedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_publication_history_indicator_entries_IndicatorEntryId",
                        column: x => x.IndicatorEntryId,
                        principalTable: "indicator_entries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "reopen_requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndicatorEntryId = table.Column<int>(type: "int", nullable: false),
                    RequestedBy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewedBy = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reopen_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reopen_requests_AspNetUsers_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_reopen_requests_AspNetUsers_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_reopen_requests_indicator_entries_IndicatorEntryId",
                        column: x => x.IndicatorEntryId,
                        principalTable: "indicator_entries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "target_values",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndicatorId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    DimensionValueId = table.Column<int>(type: "int", nullable: true),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_target_values", x => x.Id);
                    table.ForeignKey(
                        name: "FK_target_values_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_target_values_dimension_values_DimensionValueId",
                        column: x => x.DimensionValueId,
                        principalTable: "dimension_values",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_target_values_entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "entities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_target_values_indicators_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "indicators",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_publication_history_IndicatorEntryId",
                table: "publication_history",
                column: "IndicatorEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_publication_history_PerformedAt",
                table: "publication_history",
                column: "PerformedAt");

            migrationBuilder.CreateIndex(
                name: "IX_publication_history_PerformedBy",
                table: "publication_history",
                column: "PerformedBy");

            migrationBuilder.CreateIndex(
                name: "IX_reopen_requests_IndicatorEntryId",
                table: "reopen_requests",
                column: "IndicatorEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_reopen_requests_RequestedBy",
                table: "reopen_requests",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_reopen_requests_ReviewedBy",
                table: "reopen_requests",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_reopen_requests_Status",
                table: "reopen_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_target_values_CreatedBy",
                table: "target_values",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_target_values_DimensionValueId",
                table: "target_values",
                column: "DimensionValueId");

            migrationBuilder.CreateIndex(
                name: "IX_target_values_EntityId",
                table: "target_values",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_target_values_IndicatorId_Year_EntityId_DimensionValueId",
                table: "target_values",
                columns: new[] { "IndicatorId", "Year", "EntityId", "DimensionValueId" },
                unique: true,
                filter: "[EntityId] IS NOT NULL AND [DimensionValueId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "publication_history");

            migrationBuilder.DropTable(
                name: "reopen_requests");

            migrationBuilder.DropTable(
                name: "target_values");
        }
    }
}
