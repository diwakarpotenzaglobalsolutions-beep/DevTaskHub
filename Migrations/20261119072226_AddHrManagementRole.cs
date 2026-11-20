using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevTaskHubBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddHrManagementRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Roles_RoleName",
                table: "Roles");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "RoleName" },
                values: new object[] { new Guid("b6d4e8a2-1f70-4c58-9a3e-5d8c2b7f0e16"), "HR Management" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Roles_RoleName",
                table: "Roles",
                sql: "`RoleName` IN ('Admin', 'Project Manager', 'Developer', 'HR Management')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Roles_RoleName",
                table: "Roles");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: new Guid("b6d4e8a2-1f70-4c58-9a3e-5d8c2b7f0e16"));

            migrationBuilder.AddCheckConstraint(
                name: "CK_Roles_RoleName",
                table: "Roles",
                sql: "`RoleName` IN ('Admin', 'Project Manager', 'Developer')");
        }
    }
}
