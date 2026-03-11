using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GourmetApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTableKitchenFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "TableSessionItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SentToKitchen",
                table: "TableSessionItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentToKitchenAt",
                table: "TableSessionItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTableOrder",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RestaurantTableId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TableName",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TableSessionId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WaiterNotified",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TableSessionItems_OrderId",
                table: "TableSessionItems",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_TableSessionItems_Orders_OrderId",
                table: "TableSessionItems",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TableSessionItems_Orders_OrderId",
                table: "TableSessionItems");

            migrationBuilder.DropIndex(
                name: "IX_TableSessionItems_OrderId",
                table: "TableSessionItems");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "TableSessionItems");

            migrationBuilder.DropColumn(
                name: "SentToKitchen",
                table: "TableSessionItems");

            migrationBuilder.DropColumn(
                name: "SentToKitchenAt",
                table: "TableSessionItems");

            migrationBuilder.DropColumn(
                name: "IsTableOrder",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RestaurantTableId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TableName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TableSessionId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "WaiterNotified",
                table: "Orders");
        }
    }
}
