using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevTaskHubBackend.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProjectMembers",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProjectMembers");
        }
    }
}
