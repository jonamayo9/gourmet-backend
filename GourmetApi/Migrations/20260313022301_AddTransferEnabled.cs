using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GourmetApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TransferEnabled",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransferEnabled",
                table: "Companies");
        }
    }
}
