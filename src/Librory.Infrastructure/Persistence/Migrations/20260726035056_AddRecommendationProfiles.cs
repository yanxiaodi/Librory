using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendationProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recommendation_profiles",
                schema: "librory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinimumAge = table.Column<int>(type: "integer", nullable: true),
                    MaximumAge = table.Column<int>(type: "integer", nullable: true),
                    FavoriteAuthors = table.Column<string>(type: "text", nullable: false),
                    FavoriteGenres = table.Column<string>(type: "text", nullable: false),
                    FavoriteStyles = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recommendation_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recommendation_profiles_members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "librory",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recommendation_profiles_MemberId",
                schema: "librory",
                table: "recommendation_profiles",
                column: "MemberId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recommendation_profiles",
                schema: "librory");
        }
    }
}
