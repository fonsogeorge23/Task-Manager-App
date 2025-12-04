using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserIdFromTaskComments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop FK
            migrationBuilder.DropForeignKey(
                name: "FK_TaskComments_Users_UserId",
                table: "TaskComments");

            // Drop index
            migrationBuilder.DropIndex(
                name: "IX_TaskComments_UserId",
                table: "TaskComments");

            // Drop column
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TaskComments");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
        name: "UserId",
        table: "TaskComments",
        type: "int",
        nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_UserId",
                table: "TaskComments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskComments_Users_UserId",
                table: "TaskComments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

    }
}
