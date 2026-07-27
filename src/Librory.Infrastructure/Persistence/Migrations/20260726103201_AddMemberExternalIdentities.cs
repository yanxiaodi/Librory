using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberExternalIdentities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "member_external_identities",
                schema: "librory",
                columns: table => new
                {
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderSubject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "member_external_identities",
                schema: "librory");
        }
    }
}
