using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GourmetApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyPricingAndOrderBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GlobalIncreaseAmount",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GlobalIncreasePercent",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PaymentSurchargeAmount",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PaymentSurchargePercent",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalBase",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "GlobalIncreaseEnabled",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "GlobalIncreasePercent",
                table: "Companies",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "MercadoPagoSurchargeEnabled",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MercadoPagoSurchargePercent",
                table: "Companies",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "TransferSurchargeEnabled",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TransferSurchargePercent",
                table: "Companies",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GlobalIncreaseAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "GlobalIncreasePercent",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentSurchargeAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentSurchargePercent",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SubtotalBase",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "GlobalIncreaseEnabled",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "GlobalIncreasePercent",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "MercadoPagoSurchargeEnabled",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "MercadoPagoSurchargePercent",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "TransferSurchargeEnabled",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "TransferSurchargePercent",
                table: "Companies");
        }
    }
}
