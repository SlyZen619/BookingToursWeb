using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingToursWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentInfoToLocationAndLocationManagersToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocationManager",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ManagedLocationId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountName",
                table: "Locations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "Locations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Locations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentInstructions",
                table: "Locations",
                type: "nvarchar(MAX)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ManagedLocationId",
                table: "Users",
                column: "ManagedLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Locations_ManagedLocationId",
                table: "Users",
                column: "ManagedLocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Locations_ManagedLocationId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ManagedLocationId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsLocationManager",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ManagedLocationId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BankAccountName",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "PaymentInstructions",
                table: "Locations");
        }
    }
}
