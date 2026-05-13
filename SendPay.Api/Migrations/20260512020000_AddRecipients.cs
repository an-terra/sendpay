using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SendPay.Api.Migrations
{
    public partial class AddRecipients : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Recipients",
                columns: table => new
                {
                    Id        = table.Column<int>(type: "INTEGER", nullable: false)
                                    .Annotation("Sqlite:Autoincrement", true),
                    UserId    = table.Column<int>(type: "INTEGER", nullable: false),
                    Name      = table.Column<string>(type: "TEXT", nullable: false),
                    Phone     = table.Column<string>(type: "TEXT", nullable: false),
                    Note      = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recipients_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Recipients_UserId",
                table: "Recipients",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Recipients");
        }
    }
}
