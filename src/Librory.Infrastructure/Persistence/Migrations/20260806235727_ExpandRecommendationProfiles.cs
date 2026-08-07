using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandRecommendationProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExcludedAuthors",
                schema: "librory",
                table: "recommendation_profiles",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ExcludedGenres",
                schema: "librory",
                table: "recommendation_profiles",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ExcludedStyles",
                schema: "librory",
                table: "recommendation_profiles",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "PreferenceNotes",
                schema: "librory",
                table: "recommendation_profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredBookLanguages",
                schema: "librory",
                table: "recommendation_profiles",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ProfileVisibility",
                schema: "librory",
                table: "recommendation_profiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Family");

            migrationBuilder.AddColumn<bool>(
                name: "UseInFamilyRecommendations",
                schema: "librory",
                table: "recommendation_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcludedAuthors",
                schema: "librory",
                table: "recommendation_profiles");

            migrationBuilder.DropColumn(
                name: "ExcludedGenres",
                schema: "librory",
                table: "recommendation_profiles");

            migrationBuilder.DropColumn(
                name: "ExcludedStyles",
                schema: "librory",
                table: "recommendation_profiles");

            migrationBuilder.DropColumn(
                name: "PreferenceNotes",
                schema: "librory",
                table: "recommendation_profiles");

            migrationBuilder.DropColumn(
                name: "PreferredBookLanguages",
                schema: "librory",
                table: "recommendation_profiles");

            migrationBuilder.DropColumn(
                name: "ProfileVisibility",
                schema: "librory",
                table: "recommendation_profiles");

            migrationBuilder.DropColumn(
                name: "UseInFamilyRecommendations",
                schema: "librory",
                table: "recommendation_profiles");
        }
    }
}
