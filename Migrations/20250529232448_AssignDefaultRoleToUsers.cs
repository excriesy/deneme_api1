using Microsoft.EntityFrameworkCore.Migrations;

namespace ShareVault.API.Migrations
{
    public partial class AssignDefaultRoleToUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Önce varsayılan rolü ekle
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[] { "1", "User" }
            );

            // Tüm kullanıcılara varsayılan rolü ata
            migrationBuilder.Sql(@"
                INSERT INTO ""UserRoles"" (""UserId"", ""RoleId"")
                SELECT u.""Id"", '1'
                FROM ""Users"" u
                LEFT JOIN ""UserRoles"" ur ON u.""Id"" = ur.""UserId""
                WHERE ur.""UserId"" IS NULL
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Varsayılan rolü kaldır
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumns: new[] { "Id" },
                keyValues: new object[] { "1" }
            );
        }
    }
} 