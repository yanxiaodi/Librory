using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookWorks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CanonicalTitle = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CanonicalAuthor = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Summary_English = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Summary_Chinese = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookWorks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Families",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Families", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookEditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookWorkId = table.Column<Guid>(type: "uuid", nullable: false),
                    Isbn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PublicationYear = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookEditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookEditions_BookWorks_BookWorkId",
                        column: x => x.BookWorkId,
                        principalTable: "BookWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PreferredLanguage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Members_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScanSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScanSessions_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WishlistItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookWorkId = table.Column<Guid>(type: "uuid", nullable: true),
                    BookEditionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Author = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishlistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WishlistItems_BookEditions_BookEditionId",
                        column: x => x.BookEditionId,
                        principalTable: "BookEditions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WishlistItems_BookWorks_BookWorkId",
                        column: x => x.BookWorkId,
                        principalTable: "BookWorks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WishlistItems_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookCopies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookEditionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Condition = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PurchaseStore = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ShelfLocation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PurchasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookCopies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookCopies_BookEditions_BookEditionId",
                        column: x => x.BookEditionId,
                        principalTable: "BookEditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookCopies_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookCopies_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationProfile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinimumAge = table.Column<int>(type: "integer", nullable: true),
                    MaximumAge = table.Column<int>(type: "integer", nullable: true),
                    FavoriteAuthors = table.Column<string>(type: "text", nullable: false),
                    FavoriteGenres = table.Column<string>(type: "text", nullable: false),
                    FavoriteStyles = table.Column<string>(type: "text", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationProfile_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecommendationProfile_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookCopies_BookEditionId",
                table: "BookCopies",
                column: "BookEditionId");

            migrationBuilder.CreateIndex(
                name: "IX_BookCopies_FamilyId",
                table: "BookCopies",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_BookCopies_MemberId",
                table: "BookCopies",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_BookEditions_BookWorkId",
                table: "BookEditions",
                column: "BookWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_FamilyId",
                table: "Members",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationProfile_FamilyId",
                table: "RecommendationProfile",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationProfile_MemberId",
                table: "RecommendationProfile",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScanSessions_FamilyId",
                table: "ScanSessions",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_BookEditionId",
                table: "WishlistItems",
                column: "BookEditionId");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_BookWorkId",
                table: "WishlistItems",
                column: "BookWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_FamilyId",
                table: "WishlistItems",
                column: "FamilyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookCopies");

            migrationBuilder.DropTable(
                name: "RecommendationProfile");

            migrationBuilder.DropTable(
                name: "ScanSessions");

            migrationBuilder.DropTable(
                name: "WishlistItems");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "BookEditions");

            migrationBuilder.DropTable(
                name: "Families");

            migrationBuilder.DropTable(
                name: "BookWorks");
        }
    }
}
