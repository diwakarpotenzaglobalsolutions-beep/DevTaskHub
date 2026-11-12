using System;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Exceptions;
using DevTaskHubBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace DevTaskHubBackend.Services;

public interface IDashboardServices
{
    Task<DashboardResponseDto> GetDashboardAsync(Guid currentUserID, CancellationToken cancellationToken);
}

public class DashboardServices : IDashboardServices
{
    private readonly MySqlDbContext _mySqlDbContext;

    public DashboardServices(MySqlDbContext mySqlDbContext)
    {
        _mySqlDbContext = mySqlDbContext;
    }

    public async Task<DashboardResponseDto> GetDashboardAsync(
     Guid currentUserID,
     CancellationToken cancellationToken)
    {
        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted, cancellationToken);

        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or inactive.");
        }

        bool isAdmin = currentUser.RoleId == UserRole.Admin.ToId();
        bool isManager = currentUser.RoleId == UserRole.Manager.ToId();
        bool isDeveloper = currentUser.RoleId == UserRole.Developer.ToId();

        // Project Query
        var projectQuery = _mySqlDbContext.Projects
            .AsNoTracking()
            .Where(p => !p.IsDeleted);

        if (isManager)
        {
            projectQuery = projectQuery.Where(p => p.AssignedToUserId == currentUserID);
        }
        else if (isDeveloper)
        {
            projectQuery = projectQuery.Where(p => _mySqlDbContext.ProjectMembers.Any(pm => pm.ProjectId == p.ProjectId && pm.UserId == currentUserID));
        }
        else if (!isAdmin)
        {
            throw new AppException(403, "Access denied.");
        }

        // Task Query
        var taskQuery = _mySqlDbContext.Tasks.AsNoTracking().Where(t => projectQuery.Any(p => p.ProjectId == t.ProjectId));

        if (isDeveloper)
        {
            taskQuery = taskQuery.Where(t => t.AssignedToUserId == currentUserID);
        }

        int totalProjects = await projectQuery.CountAsync(cancellationToken);

        var taskCounts = await taskQuery
            .GroupBy(t => t.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        // Dashboard Response
        var dashboard = new DashboardResponseDto
        {
            TotalProjects = totalProjects,
            TotalTasks = taskCounts.Sum(x => x.Count),

            ToDoTasks = taskCounts.Where(x => x.Status == "ToDo").Sum(x => x.Count),

            InProgressTasks = taskCounts.Where(x => x.Status == "InProgress").Sum(x => x.Count),

            InReviewTasks = taskCounts.Where(x => x.Status == "InReview").Sum(x => x.Count),

            DoneTasks = taskCounts.Where(x => x.Status == "Done").Sum(x => x.Count),

            BlockedTasks = taskCounts.Where(x => x.Status == "Blocked").Sum(x => x.Count)
        };

        // Admin Only: User Statistics
        if (isAdmin)
        {
            var userCounts = await _mySqlDbContext.Users.AsNoTracking().Where(u => !u.IsDeleted).GroupBy(u => 1).Select(g => new
            {
                TotalUsers = g.Count(),
                ActiveUsers = g.Count(u => u.IsActive),
                InactiveUsers = g.Count(u => !u.IsActive),

                TotalManagers = g.Count(u =>
                    u.RoleId == UserRole.Manager.ToId()),

                ActiveManagers = g.Count(u =>
                    u.RoleId == UserRole.Manager.ToId() && u.IsActive),

                InactiveManagers = g.Count(u =>
                    u.RoleId == UserRole.Manager.ToId() && !u.IsActive),

                TotalDevelopers = g.Count(u =>
                    u.RoleId == UserRole.Developer.ToId()),

                ActiveDevelopers = g.Count(u =>
                    u.RoleId == UserRole.Developer.ToId() && u.IsActive),

                InactiveDevelopers = g.Count(u =>
                    u.RoleId == UserRole.Developer.ToId() && !u.IsActive)
            })
                .FirstOrDefaultAsync(cancellationToken);

            if (userCounts != null)
            {
                dashboard.TotalUsers = userCounts.TotalUsers;
                dashboard.ActiveUsers = userCounts.ActiveUsers;
                dashboard.InactiveUsers = userCounts.InactiveUsers;

                dashboard.TotalManagers = userCounts.TotalManagers;
                dashboard.ActiveManagers = userCounts.ActiveManagers;
                dashboard.InactiveManagers = userCounts.InactiveManagers;

                dashboard.TotalDevelopers = userCounts.TotalDevelopers;
                dashboard.ActiveDevelopers = userCounts.ActiveDevelopers;
                dashboard.InactiveDevelopers = userCounts.InactiveDevelopers;
            }
        }

        return dashboard;
    }
}
