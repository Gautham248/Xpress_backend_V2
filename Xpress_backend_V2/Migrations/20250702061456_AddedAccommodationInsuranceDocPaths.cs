using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xpress_backend_V2.Migrations
{
    /// <inheritdoc />
    public partial class AddedAccommodationInsuranceDocPaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "AccommodationDocumentPath",
                table: "TravelRequests",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "InsuranceDocumentPath",
                table: "TravelRequests",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccommodationDocumentPath",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "InsuranceDocumentPath",
                table: "TravelRequests");
        }
    }
}
