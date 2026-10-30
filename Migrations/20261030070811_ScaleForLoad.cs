using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevTaskHubBackend.Migrations
{
    /// <inheritdoc />
    public partial class ScaleForLoad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Projects_ProjectsDBProjectId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_CreatedByUserId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Users_AssignedToUserId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ManagerId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_AssignedToUserId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Projects_CreatedByUserId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ProjectsDBProjectId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectsDBProjectId",
                table: "Projects");

            migrationBuilder.RenameIndex(
                name: "IX_Roles_RoleName",
                table: "Roles",
                newName: "UX_Roles_RoleName");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<Guid>(
                name: "ManagerId",
                table: "Users",
                type: "char(36)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "char(36)");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<string>(
                name: "GitHubUserName",
                table: "Users",
                type: "varchar(39)",
                maxLength: 39,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "varchar(254)",
                maxLength: 254,
                nullable: false,
                collation: "utf8mb4_0900_ai_ci",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<uint>(
                name: "RowVersion",
                table: "Users",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Users",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.Sql("UPDATE `Tasks` SET `UpdatedAt` = `CreatedAt` WHERE `UpdatedAt` IS NULL;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tasks",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaskSummery",
                table: "Tasks",
                type: "varchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Tasks",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "ToDo",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "Tasks",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Medium",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "GitHubPRUrl",
                table: "Tasks",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GitHubPRStatus",
                table: "Tasks",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AssignedToUserId",
                table: "Tasks",
                type: "char(36)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "char(36)");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Tasks",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<uint>(
                name: "RowVersion",
                table: "Tasks",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Projects",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Projects",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "GitHubRepoName",
                table: "Projects",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GitHubOwner",
                table: "Projects",
                type: "varchar(39)",
                maxLength: 39,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Projects",
                type: "varchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Projects",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<uint>(
                name: "RowVersion",
                table: "Projects",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "ProjectMembers",
                columns: table => new
                {
                    ProjectMemberId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ProjectId = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMembers", x => x.ProjectMemberId);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.Sql(
                """
                UPDATE `Users`
                SET `Email` = LOWER(TRIM(`Email`)),
                    `PhoneNumber` = NULLIF(TRIM(`PhoneNumber`), ''),
                    `GitHubUserName` = NULLIF(TRIM(`GitHubUserName`), '');

                UPDATE `Projects`
                SET `GitHubOwner` = NULLIF(TRIM(`GitHubOwner`), ''),
                    `GitHubRepoName` = NULLIF(TRIM(`GitHubRepoName`), '');

                UPDATE `Tasks`
                SET `GitHubPRStatus` = NULLIF(TRIM(`GitHubPRStatus`), ''),
                    `GitHubPRUrl` = NULLIF(TRIM(`GitHubPRUrl`), '');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Active_CreatedAt",
                table: "Users",
                columns: new[] { "IsActive", "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role_Active",
                table: "Users",
                columns: new[] { "RoleId", "IsActive", "IsDeleted" });

            migrationBuilder.DropIndex(
                name: "IX_Users_RoleId",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "UX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Users_GitHubUserName",
                table: "Users",
                column: "GitHubUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_ManagerNotSelf",
                table: "Users",
                sql: "`ManagerId` IS NULL OR `ManagerId` <> `UserID`");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Assignee_Board",
                table: "Tasks",
                columns: new[] { "AssignedToUserId", "IsDeleted", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Project_Board",
                table: "Tasks",
                columns: new[] { "ProjectId", "IsDeleted", "Status", "DueDate" });

            migrationBuilder.DropIndex(
                name: "IX_Tasks_ProjectId",
                table: "Tasks");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Project_Created",
                table: "Tasks",
                columns: new[] { "ProjectId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_Tasks_Project_PullRequest",
                table: "Tasks",
                columns: new[] { "ProjectId", "GitHubPRNumber" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tasks_GitHubPRStatus",
                table: "Tasks",
                sql: "(`GitHubPRStatus` IS NULL OR `GitHubPRStatus` IN ('Open', 'Merged', 'Closed'))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tasks_Priority",
                table: "Tasks",
                sql: "`Priority` IN ('Low', 'Medium', 'High', 'Critical')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tasks_Status",
                table: "Tasks",
                sql: "`Status` IN ('ToDo', 'InProgress', 'InReview', 'Done', 'Blocked')");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Owner_Status",
                table: "Projects",
                columns: new[] { "CreatedByUserId", "IsDeleted", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Status_CreatedAt",
                table: "Projects",
                columns: new[] { "IsDeleted", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_Projects_GitHubRepo",
                table: "Projects",
                columns: new[] { "GitHubOwner", "GitHubRepoName" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Projects_Status",
                table: "Projects",
                sql: "`Status` IN ('Active', 'OnHold', 'Completed', 'Archived')");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_User_Project",
                table: "ProjectMembers",
                columns: new[] { "UserId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "UX_ProjectMembers_Project_User",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_CreatedByUserId",
                table: "Projects",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Users_AssignedToUserId",
                table: "Tasks",
                column: "AssignedToUserId",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ManagerId",
                table: "Users",
                column: "ManagerId",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_CreatedByUserId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Users_AssignedToUserId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ManagerId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ProjectMembers");

            migrationBuilder.DropIndex(
                name: "IX_Users_Active_CreatedAt",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.DropIndex(
                name: "IX_Users_Role_Active",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "UX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "UX_Users_GitHubUserName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "UX_Users_PhoneNumber",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_ManagerNotSelf",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_Assignee_Board",
                table: "Tasks");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ProjectId",
                table: "Tasks",
                column: "ProjectId");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_Project_Board",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_Project_Created",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "UX_Tasks_Project_PullRequest",
                table: "Tasks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tasks_GitHubPRStatus",
                table: "Tasks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tasks_Priority",
                table: "Tasks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tasks_Status",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Owner_Status",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Status_CreatedAt",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "UX_Projects_GitHubRepo",
                table: "Projects");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Projects_Status",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Projects");

            migrationBuilder.RenameIndex(
                name: "UX_Roles_RoleName",
                table: "Roles",
                newName: "IX_Roles_RoleName");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "longtext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<Guid>(
                name: "ManagerId",
                table: "Users",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "GitHubUserName",
                table: "Users",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(39)",
                oldMaxLength: 39,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(254)",
                oldMaxLength: 254);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tasks",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AlterColumn<string>(
                name: "TaskSummery",
                table: "Tasks",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Tasks",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "ToDo");

            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "Tasks",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Medium");

            migrationBuilder.AlterColumn<string>(
                name: "GitHubPRUrl",
                table: "Tasks",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GitHubPRStatus",
                table: "Tasks",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AssignedToUserId",
                table: "Tasks",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Projects",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Projects",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Active");

            migrationBuilder.AlterColumn<string>(
                name: "GitHubRepoName",
                table: "Projects",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GitHubOwner",
                table: "Projects",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(39)",
                oldMaxLength: 39,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Projects",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(4000)",
                oldMaxLength: 4000,
                oldDefaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectsDBProjectId",
                table: "Projects",
                type: "char(36)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_AssignedToUserId",
                table: "Tasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CreatedByUserId",
                table: "Projects",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectsDBProjectId",
                table: "Projects",
                column: "ProjectsDBProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Projects_ProjectsDBProjectId",
                table: "Projects",
                column: "ProjectsDBProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_CreatedByUserId",
                table: "Projects",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Users_AssignedToUserId",
                table: "Tasks",
                column: "AssignedToUserId",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ManagerId",
                table: "Users",
                column: "ManagerId",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
