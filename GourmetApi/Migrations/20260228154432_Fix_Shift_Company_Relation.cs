using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GourmetApi.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Shift_Company_Relation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shifts_Companies_CompanyId1",
                table: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_Shifts_CompanyId1",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "CompanyId1",
                table: "Shifts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId1",
                table: "Shifts",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "Id",
                keyValue: 1,
                column: "CompanyId1",
                value: null);

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "Id",
                keyValue: 2,
                column: "CompanyId1",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_CompanyId1",
                table: "Shifts",
                column: "CompanyId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Shifts_Companies_CompanyId1",
                table: "Shifts",
                column: "CompanyId1",
                principalTable: "Companies",
                principalColumn: "Id");
        }
    }
}
