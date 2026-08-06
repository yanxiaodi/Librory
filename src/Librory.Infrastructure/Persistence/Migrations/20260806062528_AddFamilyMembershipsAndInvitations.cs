using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyMembershipsAndInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "librory",
                table: "members",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserAccountId",
                schema: "librory",
                table: "members",
                type: "uuid",
                nullable: true);

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
                    SupersededByInvitationId = table.Column<Guid>(type: "uuid", nullable: true)
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

            // Preserve existing member identities while moving ownership to accounts.
            // Reusing Member.Id for the initial account keeps the migration deterministic
            // and preserves the existing one-member/one-login relationship.
            migrationBuilder.Sql("""
                INSERT INTO librory.user_accounts ("Id")
                SELECT "Id" FROM librory.members;

                UPDATE librory.members
                SET "UserAccountId" = "Id";
                """);

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

            migrationBuilder.Sql("""
                INSERT INTO librory.user_account_external_identities
                    ("Provider", "ProviderSubject", "UserAccountId", "Email", "DisplayName", "LinkedAt")
                SELECT "Provider", "ProviderSubject", "MemberId", "Email", "DisplayName", "LinkedAt"
                FROM librory.member_external_identities;
                """);

            migrationBuilder.DropTable(
                name: "member_external_identities",
                schema: "librory");

            migrationBuilder.CreateIndex(
                name: "IX_members_UserAccountId",
                schema: "librory",
                table: "members",
                column: "UserAccountId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_members_user_accounts_UserAccountId",
                schema: "librory",
                table: "members",
                column: "UserAccountId",
                principalSchema: "librory",
                principalTable: "user_accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_members_user_accounts_UserAccountId",
                schema: "librory",
                table: "members");

            migrationBuilder.DropTable(
                name: "family_invitations",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "user_account_external_identities",
                schema: "librory");

            migrationBuilder.DropTable(
                name: "user_accounts",
                schema: "librory");

            migrationBuilder.DropIndex(
                name: "IX_members_UserAccountId",
                schema: "librory",
                table: "members");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "librory",
                table: "members");

            migrationBuilder.DropColumn(
                name: "UserAccountId",
                schema: "librory",
                table: "members");

            migrationBuilder.CreateTable(
                name: "member_external_identities",
                schema: "librory",
                columns: table => new
                {
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderSubject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    LinkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_external_identities", x => new { x.MemberId, x.Provider, x.ProviderSubject });
                    table.ForeignKey(
                        name: "FK_member_external_identities_members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "librory",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_member_external_identities_Provider_ProviderSubject",
                schema: "librory",
                table: "member_external_identities",
                columns: new[] { "Provider", "ProviderSubject" },
                unique: true);
        }
    }
}
