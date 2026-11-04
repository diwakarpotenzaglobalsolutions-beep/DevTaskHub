using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevTaskHubBackend.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceProfilePhotoWithGitHubProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProfilePhoto",
                table: "Users",
                newName: "GitHubAvatarUrl");

            migrationBuilder.AddColumn<long>(
                name: "GitHubId",
                table: "Users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitHubProfileUrl",
                table: "Users",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_Users_GitHubId",
                table: "Users",
                column: "GitHubId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Users_GitHubId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GitHubId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GitHubProfileUrl",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "GitHubAvatarUrl",
                table: "Users",
                newName: "ProfilePhoto");
        }
    }
}
