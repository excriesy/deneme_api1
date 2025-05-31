using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShareVault.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedDataModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Folders_Folders_ParentFolderId",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "CanEdit",
                table: "SharedFiles");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerifiedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastLoginIP",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImage",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StorageQuota",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "StorageUsed",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "UserRoles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "AssignedBy",
                table: "UserRoles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UserRoles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AccessCount",
                table: "SharedFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccessedAt",
                table: "SharedFiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastAccessedBy",
                table: "SharedFiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Permission",
                table: "SharedFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ShareNote",
                table: "SharedFiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Roles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Roles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemRole",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Folders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Folders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Folders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Folders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Folders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MetadataText",
                table: "Folders",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Folders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Files",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Files",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Files",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptionKey",
                table: "Files",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptionMethod",
                table: "Files",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Files",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEncrypted",
                table: "Files",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MetadataText",
                table: "Files",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Files",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FileVersions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FileId = table.Column<string>(type: "text", nullable: false),
                    VersionNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ChangeNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileVersions_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileVersions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActionLink = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SharedFolders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FolderId = table.Column<string>(type: "text", nullable: false),
                    SharedByUserId = table.Column<string>(type: "text", nullable: false),
                    SharedWithUserId = table.Column<string>(type: "text", nullable: false),
                    SharedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Permission = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ShareNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastAccessedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AccessCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedFolders_Folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SharedFolders_Users_SharedByUserId",
                        column: x => x.SharedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedFolders_Users_SharedWithUserId",
                        column: x => x.SharedWithUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileVersions_FileId",
                table: "FileVersions",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_FileVersions_UserId",
                table: "FileVersions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedFolders_FolderId",
                table: "SharedFolders",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedFolders_SharedByUserId",
                table: "SharedFolders",
                column: "SharedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedFolders_SharedWithUserId",
                table: "SharedFolders",
                column: "SharedWithUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Folders_Folders_ParentFolderId",
                table: "Folders",
                column: "ParentFolderId",
                principalTable: "Folders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Folders_Folders_ParentFolderId",
                table: "Folders");

            migrationBuilder.DropTable(
                name: "FileVersions");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "SharedFolders");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginIP",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfileImage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StorageQuota",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StorageUsed",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "AssignedBy",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "AccessCount",
                table: "SharedFiles");

            migrationBuilder.DropColumn(
                name: "LastAccessedAt",
                table: "SharedFiles");

            migrationBuilder.DropColumn(
                name: "LastAccessedBy",
                table: "SharedFiles");

            migrationBuilder.DropColumn(
                name: "Permission",
                table: "SharedFiles");

            migrationBuilder.DropColumn(
                name: "ShareNote",
                table: "SharedFiles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IsSystemRole",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "MetadataText",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "EncryptionKey",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "EncryptionMethod",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "IsEncrypted",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "MetadataText",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Files");

            migrationBuilder.AddColumn<bool>(
                name: "CanEdit",
                table: "SharedFiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Folders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddForeignKey(
                name: "FK_Folders_Folders_ParentFolderId",
                table: "Folders",
                column: "ParentFolderId",
                principalTable: "Folders",
                principalColumn: "Id");
        }
    }
}
