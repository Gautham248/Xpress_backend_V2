using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xpress_backend_V2.Migrations
{
    /// <inheritdoc />
    public partial class AddedIsBillableField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBillable",
                table: "TravelRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBillable",
                table: "TravelRequests");
        }
    }
}
