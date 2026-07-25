using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScanSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scan_sessions",
                schema: "librory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShelfPhotoPath = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scan_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scan_sessions_families_FamilyId",
                        column: x => x.FamilyId,
                        principalSchema: "librory",
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scan_candidates",
                schema: "librory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScanSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Author = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    RecommendationScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    IsAlreadyOwned = table.Column<bool>(type: "boolean", nullable: false),
                    DuplicateMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConfidenceLabel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scan_candidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scan_candidates_scan_sessions_ScanSessionId",
                        column: x => x.ScanSessionId,
                        principalSchema: "librory",
                        principalTable: "scan_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_scan_candidates_ScanSessionId",
                schema: "librory",
                table: "scan_candidates",
                column: "ScanSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_scan_sessions_ExpiresAt",
                schema: "librory",
                table: "scan_sessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_scan_sessions_FamilyId_CreatedAt",
                schema: "librory",
                table: "scan_sessions",
                columns: new[] { "FamilyId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scan_candidates",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "scan_sessions",
                schema: "librory");
        }
    }
}
