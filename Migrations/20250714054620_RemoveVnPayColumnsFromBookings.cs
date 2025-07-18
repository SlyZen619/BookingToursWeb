using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingToursWeb.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVnPayColumnsFromBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "VNPayBankCode",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "VNPayOrderInfo",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "VNPayPayDate",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "VNPayResponseCode",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "VNPayTransactionStatus",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "VNPayTxnRef",
                table: "Bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Bookings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VNPayBankCode",
                table: "Bookings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VNPayOrderInfo",
                table: "Bookings",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VNPayPayDate",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VNPayResponseCode",
                table: "Bookings",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VNPayTransactionStatus",
                table: "Bookings",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VNPayTxnRef",
                table: "Bookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
