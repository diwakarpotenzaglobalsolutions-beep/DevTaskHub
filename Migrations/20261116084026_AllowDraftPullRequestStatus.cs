using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevTaskHubBackend.Migrations
{
    /// <inheritdoc />
    public partial class AllowDraftPullRequestStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tasks_GitHubPRStatus",
                table: "Tasks");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tasks_GitHubPRStatus",
                table: "Tasks",
                sql: "(`GitHubPRStatus` IS NULL OR `GitHubPRStatus` IN ('Open', 'Draft', 'Merged', 'Closed'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tasks_GitHubPRStatus",
                table: "Tasks");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tasks_GitHubPRStatus",
                table: "Tasks",
                sql: "(`GitHubPRStatus` IS NULL OR `GitHubPRStatus` IN ('Open', 'Merged', 'Closed'))");
        }
    }
}
