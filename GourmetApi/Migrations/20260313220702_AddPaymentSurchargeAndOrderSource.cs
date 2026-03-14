using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GourmetApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentSurchargeAndOrderSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QrPayload",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrReference",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CashSurchargePercent",
                table: "Companies",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QrSurchargePercent",
                table: "Companies",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QrPayload",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "QrReference",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CashSurchargePercent",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "QrSurchargePercent",
                table: "Companies");
        }
    }
}
