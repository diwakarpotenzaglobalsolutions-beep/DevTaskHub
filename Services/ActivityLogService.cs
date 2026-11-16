using DevTaskHubBackend.Data;
using DevTaskHubBackend.Entities;
using DevTaskHubBackend.Exceptions;
using DevTaskHubBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace DevTaskHubBackend.Services;

public interface IActivityLogService
{
    void Add(Guid projectId, string projectName, Guid userId, string userName, string action, string message, Guid? taskId = null, string? taskTitle = null, Guid? targetUserId = null);

    Task<ActivityLogListDto> GetLogsAsync(Guid currentUserId, int page, int pageSize, Guid? projectId, Guid? taskId, CancellationToken cancellationToken = default);
}

public class ActivityLogService : IActivityLogService
{
    private readonly MySqlDbContext _context;

    public ActivityLogService(MySqlDbContext context)
    {
        _context = context;
    }

    public void Add(Guid projectId, string projectName, Guid userId, string userName, string action, string message, Guid? taskId = null, string? taskTitle = null, Guid? targetUserId = null)
    {
        _context.ActivityLogs.Add(new ActivityLog
        {
            ActivityLogId = Guid.CreateVersion7(),
            ProjectId = projectId,
            ProjectName = Trim(projectName, 150),
            TaskId = taskId,
            TaskTitle = string.IsNullOrWhiteSpace(taskTitle) ? null : Trim(taskTitle, 200),
            UserId = userId,
            UserName = Trim(userName, 120),
            TargetUserId = targetUserId,
            Action = Trim(action, 40),
            Message = Trim(message, 500)
        });
    }

    public async Task<ActivityLogListDto> GetLogsAsync(Guid currentUserId, int page, int pageSize, Guid? projectId, Guid? taskId, CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 20;
        }

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(item => item.UserID == currentUserId && item.IsActive && !item.IsDeleted, cancellationToken);
        if (user == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or inactive.");
        }

        var query = _context.ActivityLogs.AsNoTracking().AsQueryable();
        var isAdmin = user.RoleId == UserRole.Admin.ToId();
        var isManager = user.RoleId == UserRole.Manager.ToId();
        var isDeveloper = user.RoleId == UserRole.Developer.ToId();

        if (isManager)
        {
            query = query.Where(log =>
                log.UserId == currentUserId
                || _context.Projects.IgnoreQueryFilters().Any(project => project.ProjectId == log.ProjectId && project.AssignedToUserId == currentUserId)
                || _context.Users.Any(person => person.ManagerId == currentUserId && (person.UserID == log.UserId || person.UserID == log.TargetUserId)));
        }
        else if (isDeveloper)
        {
            query = query.Where(log =>
                log.UserId == currentUserId
                || log.TargetUserId == currentUserId
                || _context.Tasks.IgnoreQueryFilters().Any(task => task.TaskId == log.TaskId && task.AssignedToUserId == currentUserId));
        }
        else if (!isAdmin)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "You do not have permission to view logs.");
        }

        if (projectId is Guid selectedProjectId && selectedProjectId != Guid.Empty)
        {
            query = query.Where(log => log.ProjectId == selectedProjectId);
        }

        if (taskId is Guid selectedTaskId && selectedTaskId != Guid.Empty)
        {
            query = query.Where(log => log.TaskId == selectedTaskId);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(log => log.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new ActivityLogDto
            {
                ActivityLogId = log.ActivityLogId,
                ProjectId = log.ProjectId,
                ProjectName = log.ProjectName,
                TaskId = log.TaskId,
                TaskTitle = log.TaskTitle,
                UserId = log.UserId,
                UserName = log.UserName,
                Action = log.Action,
                Message = log.Message,
                CreatedAt = log.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new ActivityLogListDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private static string Trim(string value, int maxLength)
    {
        var text = value?.Trim() ?? string.Empty;
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
