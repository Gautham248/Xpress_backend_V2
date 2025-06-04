using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xpress_backend_V2.Migrations
{
    /// <inheritdoc />
    public partial class AddSequenceOrderAndStatusDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SequenceOrder",
                table: "RequestStatuses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StatusDescription",
                table: "RequestStatuses",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SequenceOrder",
                table: "RequestStatuses");

            migrationBuilder.DropColumn(
                name: "StatusDescription",
                table: "RequestStatuses");
        }
    }
}
