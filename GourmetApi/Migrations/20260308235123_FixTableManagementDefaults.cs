using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GourmetApi.Migrations
{
    public partial class FixTableManagementDefaults : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""MenuItems""
                SET ""VisibleInTables"" = TRUE
                WHERE ""VisibleInTables"" = FALSE;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""Categories""
                SET ""VisibleInPublicMenu"" = TRUE
                WHERE ""VisibleInPublicMenu"" = FALSE;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""Categories""
                SET ""VisibleInTables"" = TRUE
                WHERE ""VisibleInTables"" = FALSE;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""Companies""
                SET ""EnableGuestCount"" = TRUE
                WHERE ""EnableGuestCount"" = FALSE;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}