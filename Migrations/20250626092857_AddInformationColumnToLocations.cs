using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingToursWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddInformationColumnToLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Information",
                table: "Locations",
                type: "nvarchar(MAX)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Information",
                table: "Locations");
        }
    }
}
