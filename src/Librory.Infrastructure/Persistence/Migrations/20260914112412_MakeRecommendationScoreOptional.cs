using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeRecommendationScoreOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE librory.scan_candidates SET \"RecognitionRank\" = ROUND(\"RecommendationScore\" * 1000)::integer WHERE \"RecognitionRank\" = 0 AND \"RecommendationScore\" IS NOT NULL AND \"RecommendationScore\" > 0;");

            migrationBuilder.AlterColumn<decimal>(
                name: "RecommendationScore",
                schema: "librory",
                table: "scan_candidates",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)",
                oldPrecision: 5,
                oldScale: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE librory.scan_candidates SET \"RecommendationScore\" = 0 WHERE \"RecommendationScore\" IS NULL;");

            migrationBuilder.AlterColumn<decimal>(
                name: "RecommendationScore",
                schema: "librory",
                table: "scan_candidates",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)",
                oldPrecision: 5,
                oldScale: 4,
                oldNullable: true);
        }
    }
}
