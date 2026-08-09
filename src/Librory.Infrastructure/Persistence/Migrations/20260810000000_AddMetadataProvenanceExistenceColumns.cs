using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMetadataProvenanceExistenceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "publication_year_provenance_exists",
                schema: "librory",
                table: "book_editions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "subtitle_provenance_exists",
                schema: "librory",
                table: "book_editions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "canonical_author_provenance_exists",
                schema: "librory",
                table: "book_works",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "summary_provenance_exists",
                schema: "librory",
                table: "book_works",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "publication_year_provenance_exists",
                schema: "librory",
                table: "book_editions");

            migrationBuilder.DropColumn(
                name: "subtitle_provenance_exists",
                schema: "librory",
                table: "book_editions");

            migrationBuilder.DropColumn(
                name: "canonical_author_provenance_exists",
                schema: "librory",
                table: "book_works");

            migrationBuilder.DropColumn(
                name: "summary_provenance_exists",
                schema: "librory",
                table: "book_works");
        }
    }
}