using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xpress_backend_V2.Migrations
{
    /// <inheritdoc />
    public partial class MultidocUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // We use raw SQL because Entity Framework's AlterColumn cannot generate the required USING clause.
            // The USING clause tells PostgreSQL how to convert the old data (text) to the new data type (text[]).
            // Here, we are converting the existing string into a single-element array.
            // We also handle the case where the existing value is NULL to keep it NULL.
            migrationBuilder.Sql(
                @"ALTER TABLE ""TravelRequests""
                  ALTER COLUMN ""TicketDocumentPath"" TYPE text[]
                  USING CASE
                    WHEN ""TicketDocumentPath"" IS NULL THEN NULL
                    ELSE ARRAY[""TicketDocumentPath""]
                  END;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The Down migration must also be handled with raw SQL for the reverse conversion.
            // This statement converts the array (text[]) back to a single string (text).
            // It takes the first element of the array. If the array is empty or NULL, the result will be NULL.
            // PostgreSQL arrays are 1-based, so [1] gets the first element.
            migrationBuilder.Sql(
                @"ALTER TABLE ""TravelRequests""
                  ALTER COLUMN ""TicketDocumentPath"" TYPE text
                  USING ""TicketDocumentPath""[1];"
            );
        }
    }
}