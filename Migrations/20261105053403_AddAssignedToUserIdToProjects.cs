using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevTaskHubBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedToUserIdToProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToUserId",
                table: "Projects",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("UPDATE `Projects` SET `AssignedToUserId` = `CreatedByUserId`;");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_AssignedToUserId",
                table: "Projects",
                column: "AssignedToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_AssignedToUserId",
                table: "Projects",
                column: "AssignedToUserId",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_AssignedToUserId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_AssignedToUserId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "Projects");
        }
    }
}
