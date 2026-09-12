using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PersonalDigitalVault.Api.Data;

#nullable disable

namespace PersonalDigitalVault.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260912000100_AddGoogleDriveBackupSupport")]
public partial class AddGoogleDriveBackupSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "GoogleDriveBackupConnections",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<int>(type: "int", nullable: false),
                RefreshTokenEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                AccountEmailEncrypted = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                AutoBackupEnabled = table.Column<bool>(type: "bit", nullable: false),
                AutoBackupFrequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                LastBackupAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                NextBackupAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GoogleDriveBackupConnections", x => x.Id);
                table.ForeignKey(
                    name: "FK_GoogleDriveBackupConnections_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.NoAction);
            });

        migrationBuilder.CreateIndex(
            name: "IX_GoogleDriveBackupConnections_UserId",
            table: "GoogleDriveBackupConnections",
            column: "UserId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "GoogleDriveBackupConnections");
    }
}
