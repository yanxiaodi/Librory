using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookRecognitionJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "book_recognition_jobs",
                schema: "librory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePhotoPath = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResultJson = table.Column<string>(type: "jsonb", nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_recognition_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_book_recognition_jobs_families_FamilyId",
                        column: x => x.FamilyId,
                        principalSchema: "librory",
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_book_recognition_jobs_FamilyId_CreatedAt",
                schema: "librory",
                table: "book_recognition_jobs",
                columns: new[] { "FamilyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_book_recognition_jobs_FamilyId_Status",
                schema: "librory",
                table: "book_recognition_jobs",
                columns: new[] { "FamilyId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "book_recognition_jobs",
                schema: "librory");
        }
    }
}
