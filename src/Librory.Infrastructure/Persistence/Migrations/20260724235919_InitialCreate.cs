using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "librory");

            migrationBuilder.CreateTable(
                name: "book_works",
                schema: "librory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CanonicalTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CanonicalAuthor = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    summary_english = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    summary_chinese = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    summary_source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    summary_source_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    summary_confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    summary_captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    canonical_author_source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    canonical_author_source_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    canonical_author_confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    canonical_author_captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_works", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "families",
                schema: "librory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_families", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "book_editions",
                schema: "librory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookWorkId = table.Column<Guid>(type: "uuid", nullable: false),
                    Isbn = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Format = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    subtitle_english = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    subtitle_chinese = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PublicationYear = table.Column<int>(type: "integer", nullable: true),
                    subtitle_source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    subtitle_source_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    subtitle_confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    subtitle_captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    publication_year_source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    publication_year_source_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    publication_year_confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    publication_year_captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_editions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_book_editions_book_works_BookWorkId",
                        column: x => x.BookWorkId,
                        principalSchema: "librory",
                        principalTable: "book_works",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "members",
                schema: "librory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PreferredLanguage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_members_families_FamilyId",
                        column: x => x.FamilyId,
                        principalSchema: "librory",
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wishlist_items",
                schema: "librory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookWorkId = table.Column<Guid>(type: "uuid", nullable: true),
                    BookEditionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Author = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wishlist_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wishlist_items_book_editions_BookEditionId",
                        column: x => x.BookEditionId,
                        principalSchema: "librory",
                        principalTable: "book_editions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_wishlist_items_book_works_BookWorkId",
                        column: x => x.BookWorkId,
                        principalSchema: "librory",
                        principalTable: "book_works",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_wishlist_items_families_FamilyId",
                        column: x => x.FamilyId,
                        principalSchema: "librory",
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "book_copies",
                schema: "librory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookEditionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DuplicateStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Condition = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PurchaseStore = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ShelfLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PurchasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IntakeNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_copies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_book_copies_book_editions_BookEditionId",
                        column: x => x.BookEditionId,
                        principalSchema: "librory",
                        principalTable: "book_editions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_book_copies_families_FamilyId",
                        column: x => x.FamilyId,
                        principalSchema: "librory",
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_book_copies_members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "librory",
                        principalTable: "members",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_book_copies_BookEditionId",
                schema: "librory",
                table: "book_copies",
                column: "BookEditionId");

            migrationBuilder.CreateIndex(
                name: "IX_book_copies_FamilyId",
                schema: "librory",
                table: "book_copies",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_book_copies_MemberId",
                schema: "librory",
                table: "book_copies",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_book_editions_BookWorkId",
                schema: "librory",
                table: "book_editions",
                column: "BookWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_members_FamilyId",
                schema: "librory",
                table: "members",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_items_BookEditionId",
                schema: "librory",
                table: "wishlist_items",
                column: "BookEditionId");

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_items_BookWorkId",
                schema: "librory",
                table: "wishlist_items",
                column: "BookWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_items_FamilyId",
                schema: "librory",
                table: "wishlist_items",
                column: "FamilyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "book_copies",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "wishlist_items",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "members",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "book_editions",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "families",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "book_works",
                schema: "librory");
        }
    }
}
