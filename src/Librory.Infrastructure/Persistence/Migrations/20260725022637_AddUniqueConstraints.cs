using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_members_FamilyId",
                schema: "librory",
                table: "members");

            migrationBuilder.CreateIndex(
                name: "IX_members_FamilyId_DisplayName",
                schema: "librory",
                table: "members",
                columns: new[] { "FamilyId", "DisplayName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_families_Name",
                schema: "librory",
                table: "families",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_members_FamilyId_DisplayName",
                schema: "librory",
                table: "members");

            migrationBuilder.DropIndex(
                name: "IX_families_Name",
                schema: "librory",
                table: "families");

            migrationBuilder.CreateIndex(
                name: "IX_members_FamilyId",
                schema: "librory",
                table: "members",
                column: "FamilyId");
        }
    }
}
