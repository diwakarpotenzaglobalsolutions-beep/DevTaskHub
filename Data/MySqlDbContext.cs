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
        ConfigureTaskComments(modelBuilder);
        ConfigureActivityLogs(modelBuilder);
        ConfigureAttendances(modelBuilder);
        ConfigureLeaveTypes(modelBuilder);
        ConfigureEmployeeLeaveBalances(modelBuilder);
        ConfigureLeaveRequests(modelBuilder);
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
            OneOf(nameof(Role.RoleName), UserRoles.All)));
        entity.HasData(
            new { RoleId = UserRole.Admin.ToId(), RoleName = UserRole.Admin.ToName() },
            new { RoleId = UserRole.Manager.ToId(), RoleName = UserRole.Manager.ToName() },
            new { RoleId = UserRole.Developer.ToId(), RoleName = UserRole.Developer.ToName() },
            new { RoleId = UserRole.Hr.ToId(), RoleName = UserRole.Hr.ToName() });
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();
        entity.HasQueryFilter(user => !user.IsDeleted);
        entity.Property(user => user.FullName).HasMaxLength(120).IsRequired();
        entity.Property(user => user.Email)
            .HasMaxLength(254)
            .IsRequired()
            .ForMySQLHasCollation("utf8mb4_unicode_ci");
        entity.Property(user => user.PhoneNumber).HasMaxLength(32);
        entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
        entity.Property(user => user.GitHubUserName).HasMaxLength(39);
        entity.Property(user => user.GitHubProfileUrl).HasMaxLength(200);
        entity.Property(user => user.GitHubAvatarUrl).HasMaxLength(500);
        entity.Property(user => user.IsActive).HasDefaultValue(true);
        entity.Property(user => user.IsDeleted).HasDefaultValue(false);
        entity.Property(user => user.RowVersion).IsConcurrencyToken().HasDefaultValue(0u);

        entity.HasIndex(user => user.Email).IsUnique().HasDatabaseName("UX_Users_Email");
        entity.HasIndex(user => user.PhoneNumber).IsUnique().HasDatabaseName("UX_Users_PhoneNumber");
        entity.HasIndex(user => user.GitHubUserName).IsUnique().HasDatabaseName("UX_Users_GitHubUserName");
        entity.HasIndex(user => user.GitHubId).IsUnique().HasDatabaseName("UX_Users_GitHubId");
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
        entity.Property(project => project.Status).HasMaxLength(20).IsRequired().HasDefaultValue(nameof(ProjectStatus.Active));
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
            OneOf(projectStatus, Enum.GetNames<ProjectStatus>())));

        entity.HasOne<User>().WithMany().HasForeignKey(project => project.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<User>().WithMany().HasForeignKey(project => project.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureTasks(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Tasks>();
        entity.HasQueryFilter(task => !task.IsDeleted);
        entity.Property(task => task.Title).HasMaxLength(200).IsRequired();
        entity.Property(task => task.TaskSummery).HasMaxLength(4000);
        entity.Property(task => task.Comment).HasMaxLength(2000);
        entity.Property(task => task.Status).HasMaxLength(20).IsRequired().HasDefaultValue(nameof(TaskStatus.ToDo));
        entity.Property(task => task.Priority).HasMaxLength(20).IsRequired().HasDefaultValue(nameof(TaskPriority.Medium));
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
            table.HasCheckConstraint("CK_Tasks_Status", OneOf(taskStatus, Enum.GetNames<TaskStatus>()));
            table.HasCheckConstraint("CK_Tasks_Priority", OneOf(taskPriority, Enum.GetNames<TaskPriority>()));
            table.HasCheckConstraint("CK_Tasks_GitHubPRStatus", OneOf(pullRequestStatus, Enum.GetNames<PullRequestStatus>(), allowNull: true));
        });

        entity.HasOne<Projects>().WithMany().HasForeignKey(task => task.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<User>().WithMany().HasForeignKey(task => task.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<User>().WithMany().HasForeignKey(task => task.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureProjectMembers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ProjectMember>();
        entity.HasIndex(member => new { member.ProjectId, member.UserId }).IsUnique().HasDatabaseName("UX_ProjectMembers_Project_User");
        entity.HasIndex(member => new { member.UserId, member.ProjectId }).HasDatabaseName("IX_ProjectMembers_User_Project");
        entity.HasOne<Projects>().WithMany().HasForeignKey(member => member.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<User>().WithMany().HasForeignKey(member => member.UserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureTaskComments(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<TaskComment>();
        entity.HasQueryFilter(comment => !comment.IsDeleted);
        entity.Property(comment => comment.Comment).HasMaxLength(2000).IsRequired();
        entity.Property(comment => comment.IsDeleted).HasDefaultValue(false);
        entity.HasIndex(comment => new { comment.TaskId, comment.CreatedAt }).HasDatabaseName("IX_TaskComments_Task_Created");
        entity.HasIndex(comment => comment.UserId).HasDatabaseName("IX_TaskComments_User");
        entity.HasOne<Tasks>().WithMany().HasForeignKey(comment => comment.TaskId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<User>().WithMany().HasForeignKey(comment => comment.UserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureActivityLogs(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ActivityLog>();
        entity.Property(log => log.ProjectName).HasMaxLength(150).IsRequired();
        entity.Property(log => log.TaskTitle).HasMaxLength(200);
        entity.Property(log => log.UserName).HasMaxLength(120).IsRequired();
        entity.Property(log => log.Action).HasMaxLength(40).IsRequired();
        entity.Property(log => log.Message).HasMaxLength(500).IsRequired();
        entity.HasIndex(log => new { log.ProjectId, log.CreatedAt }).HasDatabaseName("IX_ActivityLogs_Project_Created");
        entity.HasIndex(log => new { log.UserId, log.CreatedAt }).HasDatabaseName("IX_ActivityLogs_User_Created");
        entity.HasIndex(log => log.TargetUserId).HasDatabaseName("IX_ActivityLogs_TargetUser");
        entity.HasOne<Projects>().WithMany().HasForeignKey(log => log.ProjectId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<User>().WithMany().HasForeignKey(log => log.UserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<User>().WithMany().HasForeignKey(log => log.TargetUserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureAttendances(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Attendance>();
        entity.Property(attendance => attendance.AttendanceDate)
            .HasConversion<DateOnlyConverter>()
            .HasColumnType("date");
        entity.Property(attendance => attendance.CheckInTime).HasColumnType("datetime(6)");
        entity.Property(attendance => attendance.CheckOutTime).HasColumnType("datetime(6)");
        entity.Property(attendance => attendance.WorkingMinutes).HasDefaultValue(0);
        entity.Property(attendance => attendance.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Present");
        entity.Property(attendance => attendance.Remarks).HasMaxLength(500);
        entity.Property(attendance => attendance.CreatedAt).HasColumnType("datetime(6)");
        entity.Property(attendance => attendance.UpdatedAt).HasColumnType("datetime(6)");
        entity.HasIndex(attendance => new { attendance.UserId, attendance.AttendanceDate })
            .IsUnique()
            .HasDatabaseName("UX_Attendances_User_Date");
        entity.HasOne<User>().WithMany().HasForeignKey(attendance => attendance.UserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureLeaveTypes(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<LeaveTypes>();
        entity.HasKey(leaveType => leaveType.LeaveTypeId);
        entity.Property(leaveType => leaveType.AnnualQuota).HasPrecision(10, 2);
        entity.Property(leaveType => leaveType.IsActive).HasDefaultValue(true);
        entity.Property(leaveType => leaveType.CreatedAt).HasColumnType("datetime(6)");
        entity.Property(leaveType => leaveType.UpdatedAt).HasColumnType("datetime(6)");
    }

    private static void ConfigureEmployeeLeaveBalances(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EmployeeLeaveBalances>();
        entity.HasKey(balance => balance.EmployeeLeaveBalanceId);
        entity.Property(balance => balance.AllocatedDays).HasPrecision(10, 2);
        entity.Property(balance => balance.UsedDays).HasPrecision(10, 2);
        entity.Property(balance => balance.CreatedAt).HasColumnType("datetime(6)");
        entity.Property(balance => balance.UpdatedAt).HasColumnType("datetime(6)");
        entity.HasIndex(balance => new { balance.UserId, balance.LeaveTypeId, balance.Year })
            .IsUnique()
            .HasDatabaseName("UX_EmployeeLeaveBalances_User_LeaveType_Year");
        entity.HasOne(balance => balance.User).WithMany().HasForeignKey(balance => balance.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(balance => balance.LeaveType).WithMany().HasForeignKey(balance => balance.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureLeaveRequests(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<LeaveRequests>();
        entity.HasKey(request => request.LeaveRequestId);
        entity.Property(request => request.StartDate).HasConversion<DateOnlyConverter>().HasColumnType("date");
        entity.Property(request => request.EndDate).HasConversion<DateOnlyConverter>().HasColumnType("date");
        entity.Property(request => request.TotalDays).HasPrecision(10, 2);
        entity.Property(request => request.Status).HasMaxLength(20).HasDefaultValue("Pending");
        entity.Property(request => request.ReviewedAt).HasColumnType("datetime(6)");
        entity.Property(request => request.CreatedAt).HasColumnType("datetime(6)");
        entity.Property(request => request.UpdatedAt).HasColumnType("datetime(6)");
        entity.HasOne(request => request.User).WithMany().HasForeignKey(request => request.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(request => request.LeaveType).WithMany().HasForeignKey(request => request.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(request => request.ReviewedByUser).WithMany().HasForeignKey(request => request.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
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
