using Microsoft.EntityFrameworkCore.Migrations;

namespace BlazorChat.Server.Data.Migrations
{
    public partial class AddedChatStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ChatMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "ChatMessages");
        }
    }
}
