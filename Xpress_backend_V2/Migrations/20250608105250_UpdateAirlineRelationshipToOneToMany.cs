using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xpress_backend_V2.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAirlineRelationshipToOneToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, remove the old foreign key constraint. This is safe.
            migrationBuilder.DropForeignKey(
                name: "FK_TravelRequests_Airlines_AirlineId",
                table: "TravelRequests");

            migrationBuilder.DropIndex(
                name: "IX_TravelRequests_AirlineId",
                table: "TravelRequests");

            // STEP 1: Add the new column to the 'Airlines' table.
            migrationBuilder.AddColumn<string>(
                name: "RequestId",
                table: "Airlines",
                type: "text", // Correct for PostgreSQL
                nullable: true);

            // ***************************************************************
            // ** STEP 2: INSERT THE CUSTOM DATA MIGRATION SCRIPT **
            // This runs after the new column exists but before the old one is gone.
            migrationBuilder.Sql(
                @"
                UPDATE ""Airlines"" AS a
                SET ""RequestId"" = tr.""RequestId""
                FROM ""TravelRequests"" AS tr
                WHERE a.""AirlineId"" = tr.""AirlineId"" AND tr.""AirlineId"" IS NOT NULL;
                "
            );
            // ***************************************************************

            // STEP 3: Now that data is copied, it's safe to drop the old column.
            migrationBuilder.DropColumn(
                name: "AirlineId",
                table: "TravelRequests");

            // STEP 4: Create the new index and foreign key for the new relationship.
            migrationBuilder.CreateIndex(
                name: "IX_Airlines_RequestId",
                table: "Airlines",
                column: "RequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Airlines_TravelRequests_RequestId",
                table: "Airlines",
                column: "RequestId",
                principalTable: "TravelRequests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The auto-generated Down method is generally fine. It will revert the
            // schema changes but will not be able to restore the data relationship.
            // This is usually an acceptable limitation for a Down migration.
            migrationBuilder.DropForeignKey(
                name: "FK_Airlines_TravelRequests_RequestId",
                table: "Airlines");

            migrationBuilder.DropIndex(
                name: "IX_Airlines_RequestId",
                table: "Airlines");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "Airlines");

            migrationBuilder.AddColumn<int>(
                name: "AirlineId",
                table: "TravelRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_AirlineId",
                table: "TravelRequests",
                column: "AirlineId");

            migrationBuilder.AddForeignKey(
                name: "FK_TravelRequests_Airlines_AirlineId",
                table: "TravelRequests",
                column: "AirlineId",
                principalTable: "Airlines",
                principalColumn: "AirlineId");
        }
    }
}