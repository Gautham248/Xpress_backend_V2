using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xpress_backend_V2.Migrations
{
    /// <inheritdoc />
    public partial class PickupDropoffAddedToTravelRequestTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DropOffPlace",
                table: "TravelRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickUpPlace",
                table: "TravelRequests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DropOffPlace",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "PickUpPlace",
                table: "TravelRequests");
        }
    }
}
