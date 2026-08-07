using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScanRecommendationContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasMixedLanguages",
                schema: "librory",
                table: "scan_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "InferredLanguage",
                schema: "librory",
                table: "scan_sessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TargetMemberId",
                schema: "librory",
                table: "scan_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TargetProfileAvailable",
                schema: "librory",
                table: "scan_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TargetProfileUsed",
                schema: "librory",
                table: "scan_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DetectedLanguage",
                schema: "librory",
                table: "scan_candidates",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_scan_sessions_FamilyId_TargetMemberId",
                schema: "librory",
                table: "scan_sessions",
                columns: new[] { "FamilyId", "TargetMemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_scan_sessions_TargetMemberId",
                schema: "librory",
                table: "scan_sessions",
                column: "TargetMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_scan_sessions_members_TargetMemberId",
                schema: "librory",
                table: "scan_sessions",
                column: "TargetMemberId",
                principalSchema: "librory",
                principalTable: "members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_scan_sessions_members_TargetMemberId",
                schema: "librory",
                table: "scan_sessions");

            migrationBuilder.DropIndex(
                name: "IX_scan_sessions_FamilyId_TargetMemberId",
                schema: "librory",
                table: "scan_sessions");

            migrationBuilder.DropIndex(
                name: "IX_scan_sessions_TargetMemberId",
                schema: "librory",
                table: "scan_sessions");

            migrationBuilder.DropColumn(
                name: "HasMixedLanguages",
                schema: "librory",
                table: "scan_sessions");

            migrationBuilder.DropColumn(
                name: "InferredLanguage",
                schema: "librory",
                table: "scan_sessions");

            migrationBuilder.DropColumn(
                name: "TargetMemberId",
                schema: "librory",
                table: "scan_sessions");

            migrationBuilder.DropColumn(
                name: "TargetProfileAvailable",
                schema: "librory",
                table: "scan_sessions");

            migrationBuilder.DropColumn(
                name: "TargetProfileUsed",
                schema: "librory",
                table: "scan_sessions");

            migrationBuilder.DropColumn(
                name: "DetectedLanguage",
                schema: "librory",
                table: "scan_candidates");
        }
    }
}
