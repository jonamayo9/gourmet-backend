using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GourmetApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCanAccessCompanySettingsToAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanAccessCompanySettings",
                table: "AdminUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanAccessCompanySettings",
                table: "AdminUsers");
        }
    }
}
