using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyInvitationConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL supplies xmin as a system column; no physical column is added.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL supplies xmin as a system column; no physical column is removed.
        }
    }
}
