using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GourmetApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentSurcharges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GlobalIncreaseAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "GlobalIncreasePercent",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "GlobalIncreaseEnabled",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "GlobalIncreasePercent",
                table: "Companies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
