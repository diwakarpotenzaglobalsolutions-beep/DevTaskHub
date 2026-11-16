using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevTaskHubBackend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTaskCommentLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskCommentLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskCommentLogs",
                columns: table => new
                {
                    TaskCommentLogId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Action = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Comment = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TaskCommentId = table.Column<Guid>(type: "char(36)", nullable: false),
                    TaskId = table.Column<Guid>(type: "char(36)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCommentLogs", x => x.TaskCommentLogId);
                    table.ForeignKey(
                        name: "FK_TaskCommentLogs_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskCommentLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TaskCommentLogs_Task_Created",
                table: "TaskCommentLogs",
                columns: new[] { "TaskId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskCommentLogs_UserId",
                table: "TaskCommentLogs",
                column: "UserId");
        }
    }
}
