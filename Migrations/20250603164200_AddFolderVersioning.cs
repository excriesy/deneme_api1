using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShareVault.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFolderVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "Folders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "Files",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "FolderVersions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FolderId = table.Column<string>(type: "text", nullable: false),
                    VersionNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ChangeNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StructureHash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolderVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FolderVersions_Folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FolderVersions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FolderVersions_FolderId",
                table: "FolderVersions",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_FolderVersions_UserId",
                table: "FolderVersions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FolderVersions");

            migrationBuilder.DropColumn(
                name: "Path",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "Files");
        }
    }
}
