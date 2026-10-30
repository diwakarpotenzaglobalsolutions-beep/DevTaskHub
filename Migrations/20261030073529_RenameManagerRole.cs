using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevTaskHubBackend.Migrations
{
    /// <inheritdoc />
    public partial class RenameManagerRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Roles_RoleName",
                table: "Roles");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: new Guid("3c9e1b74-2a68-4f05-b8d3-6e1c9a4d7b52"),
                column: "RoleName",
                value: "Project Manager");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Roles_RoleName",
                table: "Roles",
                sql: "`RoleName` IN ('Admin', 'Project Manager', 'Developer')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Roles_RoleName",
                table: "Roles");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: new Guid("3c9e1b74-2a68-4f05-b8d3-6e1c9a4d7b52"),
                column: "RoleName",
                value: "Manager");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Roles_RoleName",
                table: "Roles",
                sql: "`RoleName` IN ('Admin', 'Manager', 'Developer')");
        }
    }
}
