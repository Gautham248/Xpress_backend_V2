using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xpress_backend_V2.Migrations
{
    /// <inheritdoc />
    public partial class FixTicketDocumentPathToJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"ALTER TABLE ""TravelRequests""
                  ALTER COLUMN ""TicketDocumentPath"" TYPE jsonb;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"ALTER TABLE ""TravelRequests""
                  ALTER COLUMN ""TicketDocumentPath"" TYPE text;"
            );
        }
    }
}
