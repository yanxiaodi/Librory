using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScanPurchaseFlowState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MetadataMatchesJson",
                schema: "librory",
                table: "scan_candidates",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseRequestId",
                schema: "librory",
                table: "scan_candidates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseStatus",
                schema: "librory",
                table: "scan_candidates",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PurchasedAt",
                schema: "librory",
                table: "scan_candidates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PurchasedBookCopyId",
                schema: "librory",
                table: "scan_candidates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecognitionRank",
                schema: "librory",
                table: "scan_candidates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "UsePrivateNotesInFamilyRecommendations",
                schema: "librory",
                table: "recommendation_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsProvisional",
                schema: "librory",
                table: "book_editions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PurchasedByMemberId",
                schema: "librory",
                table: "book_copies",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_scan_candidates_PurchaseRequestId",
                schema: "librory",
                table: "scan_candidates",
                column: "PurchaseRequestId",
                unique: true,
                filter: "\"PurchaseRequestId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_book_copies_PurchasedByMemberId",
                schema: "librory",
                table: "book_copies",
                column: "PurchasedByMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_book_copies_members_PurchasedByMemberId",
                schema: "librory",
                table: "book_copies",
                column: "PurchasedByMemberId",
                principalSchema: "librory",
                principalTable: "members",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_book_copies_members_PurchasedByMemberId",
                schema: "librory",
                table: "book_copies");

            migrationBuilder.DropIndex(
                name: "IX_scan_candidates_PurchaseRequestId",
                schema: "librory",
                table: "scan_candidates");

            migrationBuilder.DropIndex(
                name: "IX_book_copies_PurchasedByMemberId",
                schema: "librory",
                table: "book_copies");

            migrationBuilder.DropColumn(
                name: "MetadataMatchesJson",
                schema: "librory",
                table: "scan_candidates");

            migrationBuilder.DropColumn(
                name: "PurchaseRequestId",
                schema: "librory",
                table: "scan_candidates");

            migrationBuilder.DropColumn(
                name: "PurchaseStatus",
                schema: "librory",
                table: "scan_candidates");

            migrationBuilder.DropColumn(
                name: "PurchasedAt",
                schema: "librory",
                table: "scan_candidates");

            migrationBuilder.DropColumn(
                name: "PurchasedBookCopyId",
                schema: "librory",
                table: "scan_candidates");

            migrationBuilder.DropColumn(
                name: "RecognitionRank",
                schema: "librory",
                table: "scan_candidates");

            migrationBuilder.DropColumn(
                name: "UsePrivateNotesInFamilyRecommendations",
                schema: "librory",
                table: "recommendation_profiles");

            migrationBuilder.DropColumn(
                name: "IsProvisional",
                schema: "librory",
                table: "book_editions");

            migrationBuilder.DropColumn(
                name: "PurchasedByMemberId",
                schema: "librory",
                table: "book_copies");
        }
    }
}
