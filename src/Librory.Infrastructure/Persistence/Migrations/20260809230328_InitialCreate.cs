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
                    summary_provenance_exists = table.Column<bool>(type: "boolean", nullable: true),
                    summary_source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    summary_source_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    summary_confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    summary_captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    canonical_author_provenance_exists = table.Column<bool>(type: "boolean", nullable: true),
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
                name: "user_accounts",
                schema: "librory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_accounts", x => x.Id);
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
                    subtitle_provenance_exists = table.Column<bool>(type: "boolean", nullable: true),
                    subtitle_source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    subtitle_source_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    subtitle_confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    subtitle_captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    publication_year_provenance_exists = table.Column<bool>(type: "boolean", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "members",
                schema: "librory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PreferredLanguage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
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
                    table.ForeignKey(
                        name: "FK_members_user_accounts_UserAccountId",
                        column: x => x.UserAccountId,
                        principalSchema: "librory",
                        principalTable: "user_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_account_external_identities",
                schema: "librory",
                columns: table => new
                {
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderSubject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LinkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_account_external_identities", x => new { x.UserAccountId, x.Provider, x.ProviderSubject });
                    table.ForeignKey(
                        name: "FK_user_account_external_identities_user_accounts_UserAccountId",
                        column: x => x.UserAccountId,
                        principalSchema: "librory",
                        principalTable: "user_accounts",
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

            migrationBuilder.CreateTable(
                name: "family_invitations",
                schema: "librory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedByMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptedAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedByMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SupersededByInvitationId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family_invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_family_invitations_families_FamilyId",
                        column: x => x.FamilyId,
                        principalSchema: "librory",
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_family_invitations_members_CreatedByMemberId",
                        column: x => x.CreatedByMemberId,
                        principalSchema: "librory",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_family_invitations_members_RevokedByMemberId",
                        column: x => x.RevokedByMemberId,
                        principalSchema: "librory",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_family_invitations_members_TargetMemberId",
                        column: x => x.TargetMemberId,
                        principalSchema: "librory",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_family_invitations_user_accounts_AcceptedAccountId",
                        column: x => x.AcceptedAccountId,
                        principalSchema: "librory",
                        principalTable: "user_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                    ExcludedAuthors = table.Column<string>(type: "text", nullable: false),
                    FavoriteGenres = table.Column<string>(type: "text", nullable: false),
                    ExcludedGenres = table.Column<string>(type: "text", nullable: false),
                    FavoriteStyles = table.Column<string>(type: "text", nullable: false),
                    ExcludedStyles = table.Column<string>(type: "text", nullable: false),
                    PreferredBookLanguages = table.Column<string>(type: "text", nullable: false),
                    PreferenceNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProfileVisibility = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UseInFamilyRecommendations = table.Column<bool>(type: "boolean", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "scan_sessions",
                schema: "librory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShelfPhotoPath = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TargetMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetProfileAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    TargetProfileUsed = table.Column<bool>(type: "boolean", nullable: false),
                    InferredLanguage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    HasMixedLanguages = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scan_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scan_sessions_families_FamilyId",
                        column: x => x.FamilyId,
                        principalSchema: "librory",
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_scan_sessions_members_TargetMemberId",
                        column: x => x.TargetMemberId,
                        principalSchema: "librory",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scan_candidates",
                schema: "librory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScanSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Author = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    RecommendationScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    IsAlreadyOwned = table.Column<bool>(type: "boolean", nullable: false),
                    DuplicateMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConfidenceLabel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DetectedLanguage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scan_candidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scan_candidates_scan_sessions_ScanSessionId",
                        column: x => x.ScanSessionId,
                        principalSchema: "librory",
                        principalTable: "scan_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_book_recognition_jobs_FamilyId_CreatedAt",
                schema: "librory",
                table: "book_recognition_jobs",
                columns: new[] { "FamilyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_book_recognition_jobs_FamilyId_Status",
                schema: "librory",
                table: "book_recognition_jobs",
                columns: new[] { "FamilyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_families_Name",
                schema: "librory",
                table: "families",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_family_invitations_AcceptedAccountId",
                schema: "librory",
                table: "family_invitations",
                column: "AcceptedAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_family_invitations_CreatedByMemberId",
                schema: "librory",
                table: "family_invitations",
                column: "CreatedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_family_invitations_ExpiresAt",
                schema: "librory",
                table: "family_invitations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_family_invitations_FamilyId_Email_Status",
                schema: "librory",
                table: "family_invitations",
                columns: new[] { "FamilyId", "Email", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_family_invitations_RevokedByMemberId",
                schema: "librory",
                table: "family_invitations",
                column: "RevokedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_family_invitations_TargetMemberId",
                schema: "librory",
                table: "family_invitations",
                column: "TargetMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_family_invitations_TokenHash",
                schema: "librory",
                table: "family_invitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_members_FamilyId_DisplayName",
                schema: "librory",
                table: "members",
                columns: new[] { "FamilyId", "DisplayName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_members_UserAccountId",
                schema: "librory",
                table: "members",
                column: "UserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_recommendation_profiles_MemberId",
                schema: "librory",
                table: "recommendation_profiles",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scan_candidates_ScanSessionId",
                schema: "librory",
                table: "scan_candidates",
                column: "ScanSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_scan_sessions_ExpiresAt",
                schema: "librory",
                table: "scan_sessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_scan_sessions_FamilyId_CreatedAt",
                schema: "librory",
                table: "scan_sessions",
                columns: new[] { "FamilyId", "CreatedAt" });

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

            migrationBuilder.CreateIndex(
                name: "IX_user_account_external_identities_Provider_ProviderSubject",
                schema: "librory",
                table: "user_account_external_identities",
                columns: new[] { "Provider", "ProviderSubject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_Email",
                schema: "librory",
                table: "user_accounts",
                column: "Email",
                unique: true);

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
                name: "book_recognition_jobs",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "family_invitations",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "recommendation_profiles",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "scan_candidates",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "user_account_external_identities",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "wishlist_items",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "scan_sessions",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "book_editions",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "members",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "book_works",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "families",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "user_accounts",
                schema: "librory");
        }
    }
}
