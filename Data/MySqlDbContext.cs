using DevTaskHubBackend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using MySql.EntityFrameworkCore.Extensions;

namespace DevTaskHubBackend.Data;

public class MySqlDbContext : MyDbContext
{
    public MySqlDbContext(DbContextOptions<MySqlDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<BaseEntity>();
        ConfigureRoles(modelBuilder);
        ConfigureUsers(modelBuilder);
        ConfigureProjects(modelBuilder);
        ConfigureTasks(modelBuilder);
        ConfigureProjectMembers(modelBuilder);
        ConfigureAuditColumns(modelBuilder);
        UseUuid7Keys(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureRoles(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Role>();
        entity.Property(role => role.RoleName).HasMaxLength(50).IsRequired();
        entity.HasIndex(role => role.RoleName)
            .IsUnique()
            .HasDatabaseName("UX_Roles_RoleName");
        entity.ToTable(table => table.HasCheckConstraint(
            "CK_Roles_RoleName",
            OneOf(nameof(Role.RoleName), [UserRoles.Admin, UserRoles.Manager, UserRoles.Developer])));
        entity.HasData(
            new { RoleId = UserRoles.AdminId, RoleName = UserRoles.Admin },
            new { RoleId = UserRoles.ManagerId, RoleName = UserRoles.Manager },
            new { RoleId = UserRoles.DeveloperId, RoleName = UserRoles.Developer });
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();
        entity.HasQueryFilter(user => !user.IsDeleted);
        entity.Property(user => user.FullName).HasMaxLength(120).IsRequired();
        entity.Property(user => user.Email)
            .HasMaxLength(254)
            .IsRequired()
            .ForMySQLHasCollation("utf8mb4_0900_ai_ci");
        entity.Property(user => user.PhoneNumber).HasMaxLength(32);
        entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
        entity.Property(user => user.GitHubUserName).HasMaxLength(39);
        entity.Property(user => user.IsActive).HasDefaultValue(true);
        entity.Property(user => user.IsDeleted).HasDefaultValue(false);
        entity.Property(user => user.RowVersion).IsConcurrencyToken().HasDefaultValue(0u);

        entity.HasIndex(user => user.Email).IsUnique().HasDatabaseName("UX_Users_Email");
        entity.HasIndex(user => user.PhoneNumber).IsUnique().HasDatabaseName("UX_Users_PhoneNumber");
        entity.HasIndex(user => user.GitHubUserName).IsUnique().HasDatabaseName("UX_Users_GitHubUserName");
        entity.HasIndex(user => new { user.RoleId, user.IsActive, user.IsDeleted }).HasDatabaseName("IX_Users_Role_Active");
        entity.HasIndex(user => user.ManagerId).HasDatabaseName("IX_Users_ManagerId");
        entity.HasIndex(user => new { user.IsActive, user.IsDeleted, user.CreatedAt }).HasDatabaseName("IX_Users_Active_CreatedAt");

        entity.ToTable(table => table.HasCheckConstraint(
            "CK_Users_ManagerNotSelf",
            "`ManagerId` IS NULL OR `ManagerId` <> `UserID`"));

        entity.HasOne<Role>().WithMany().HasForeignKey(user => user.RoleId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<User>().WithMany().HasForeignKey(user => user.ManagerId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureProjects(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Projects>();
        entity.HasQueryFilter(project => !project.IsDeleted);
        entity.Property(project => project.ProjectName).HasMaxLength(150).IsRequired();
        entity.Property(project => project.Description).HasMaxLength(4000).IsRequired().HasDefaultValue(string.Empty);
        entity.Property(project => project.Status).HasMaxLength(20).IsRequired().HasDefaultValue(ProjectStatuses.Active);
        entity.Property(project => project.GitHubOwner).HasMaxLength(39);
        entity.Property(project => project.GitHubRepoName).HasMaxLength(100);
        entity.Property(project => project.IsDeleted).HasDefaultValue(false);
        entity.Property(project => project.RowVersion).IsConcurrencyToken().HasDefaultValue(0u);

        entity.HasIndex(project => new { project.CreatedByUserId, project.IsDeleted, project.Status }).HasDatabaseName("IX_Projects_Owner_Status");
        entity.HasIndex(project => new { project.IsDeleted, project.Status, project.CreatedAt }).HasDatabaseName("IX_Projects_Status_CreatedAt");
        entity.HasIndex(project => new { project.GitHubOwner, project.GitHubRepoName }).IsUnique().HasDatabaseName("UX_Projects_GitHubRepo");

        var projectStatus = entity.Property(project => project.Status).Metadata.Name;
        entity.ToTable(table => table.HasCheckConstraint(
            "CK_Projects_Status",
            OneOf(projectStatus, ProjectStatuses.All)));

        entity.HasOne<User>().WithMany().HasForeignKey(project => project.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureTasks(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Tasks>();
        entity.HasQueryFilter(task => !task.IsDeleted);
        entity.Property(task => task.Title).HasMaxLength(200).IsRequired();
        entity.Property(task => task.TaskSummery).HasMaxLength(4000);
        entity.Property(task => task.Status).HasMaxLength(20).IsRequired().HasDefaultValue(TaskStatuses.ToDo);
        entity.Property(task => task.Priority).HasMaxLength(20).IsRequired().HasDefaultValue(TaskPriorities.Medium);
        entity.Property(task => task.GitHubPRStatus).HasMaxLength(20);
        entity.Property(task => task.GitHubPRUrl).HasMaxLength(500);
        entity.Property(task => task.IsDeleted).HasDefaultValue(false);
        entity.Property(task => task.DueDate).HasColumnType("datetime(6)");
        entity.Property(task => task.LastSyncedAt).HasColumnType("datetime(6)");
        entity.Property(task => task.RowVersion).IsConcurrencyToken().HasDefaultValue(0u);

        entity.HasIndex(task => new { task.ProjectId, task.IsDeleted, task.Status, task.DueDate }).HasDatabaseName("IX_Tasks_Project_Board");
        entity.HasIndex(task => new { task.AssignedToUserId, task.IsDeleted, task.Status, task.DueDate }).HasDatabaseName("IX_Tasks_Assignee_Board");
        entity.HasIndex(task => new { task.ProjectId, task.CreatedAt }).HasDatabaseName("IX_Tasks_Project_Created");
        entity.HasIndex(task => new { task.ProjectId, task.GitHubPRNumber }).IsUnique().HasDatabaseName("UX_Tasks_Project_PullRequest");

        var taskStatus = entity.Property(task => task.Status).Metadata.Name;
        var taskPriority = entity.Property(task => task.Priority).Metadata.Name;
        var pullRequestStatus = entity.Property(task => task.GitHubPRStatus).Metadata.Name;
        entity.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Tasks_Status", OneOf(taskStatus, TaskStatuses.All));
            table.HasCheckConstraint("CK_Tasks_Priority", OneOf(taskPriority, TaskPriorities.All));
            table.HasCheckConstraint("CK_Tasks_GitHubPRStatus", OneOf(pullRequestStatus, PullRequestStatuses.All, allowNull: true));
        });

        entity.HasOne<Projects>().WithMany().HasForeignKey(task => task.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<User>().WithMany().HasForeignKey(task => task.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureProjectMembers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ProjectMember>();
        entity.HasIndex(member => new { member.ProjectId, member.UserId }).IsUnique().HasDatabaseName("UX_ProjectMembers_Project_User");
        entity.HasIndex(member => new { member.UserId, member.ProjectId }).HasDatabaseName("IX_ProjectMembers_User_Project");
        entity.HasOne<Projects>().WithMany().HasForeignKey(member => member.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<User>().WithMany().HasForeignKey(member => member.UserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureAuditColumns(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var entity = modelBuilder.Entity(entityType.ClrType);
            entity.Property(nameof(BaseEntity.CreatedAt)).HasColumnType("datetime(6)");
            entity.Property(nameof(BaseEntity.UpdatedAt))
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
        }
    }

    private static void UseUuid7Keys(ModelBuilder modelBuilder)
    {
        foreach (var property in modelBuilder.Model
            .GetEntityTypes()
            .Select(entity => entity.FindPrimaryKey())
            .Where(key => key is { Properties.Count: 1 })
            .Select(key => key!.Properties[0])
            .Where(property => property.ClrType == typeof(Guid)))
        {
            property.SetValueGeneratorFactory((_, _) => new Uuid7ValueGenerator());
        }
    }

    private static string OneOf(string column, IEnumerable<string> values, bool allowNull = false)
    {
        var list = string.Join(", ", values.Select(value => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'"));
        var membership = $"`{column}` IN ({list})";
        return allowNull ? $"(`{column}` IS NULL OR {membership})" : membership;
    }

    private sealed class Uuid7ValueGenerator : ValueGenerator<Guid>
    {
        public override bool GeneratesTemporaryValues => false;

        public override Guid Next(EntityEntry entry) => Guid.CreateVersion7();
    }
}
