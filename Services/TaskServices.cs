using System;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Entities;
using DevTaskHubBackend.Exceptions;
using DevTaskHubBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace DevTaskHubBackend.Services;

public interface ITaskServices
{
    Task<bool> CreateTaskAsync(CreateTaskDto createTaskDto, Guid currentUserID, CancellationToken cancellationToken = default);
    Task<List<GetTaskResponseDto>> GetTaskAsync(Guid currentUserID, CancellationToken cancellationToken = default);

    Task<bool> EditTaskAsync(Guid taskID, EditTaskDto editTaskDto, Guid currentUserID, CancellationToken cancellationToken = default);

    Task<bool> AssignTaskAsync(AssignTaskDto assignTaskDto, Guid currentUserID, CancellationToken cancellationToken = default);
    Task<bool> UpdateTaskStatusAsync(Guid taskID, UpdateTaskStatusDto dto, Guid currentUserID, CancellationToken cancellationToken);
}

public class TaskServices : ITaskServices
{
    private readonly MySqlDbContext _mySqlDbContext;

    public TaskServices(MySqlDbContext mySqlDbContext)
    {
        _mySqlDbContext = mySqlDbContext;
    }

    public async Task<bool> CreateTaskAsync(CreateTaskDto createTaskDto, Guid currentUserID, CancellationToken cancellationToken)
    {
        var project = await _mySqlDbContext.Projects.FirstOrDefaultAsync(p => p.ProjectId == createTaskDto.ProjectID && !p.IsDeleted, cancellationToken);
        if (project == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Project not found");
        }
        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted, cancellationToken);
        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or inActive");
        }

        bool isAdmin = currentUser.RoleId == UserRole.Admin.ToId();
        if (!isAdmin && project.AssignedToUserId != currentUserID)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "This project is not assigned to you.");
        }

        var task = new Tasks
        {
            TaskId = Guid.CreateVersion7(),
            ProjectId = createTaskDto.ProjectID,
            Title = createTaskDto.Title.Trim(),
            TaskSummery = createTaskDto.Description?.Trim(),
            Priority = createTaskDto.Priority.ToString(),
            Status = Data.TaskStatus.ToDo.ToString(),
            AssignedToUserId = null,
            CreatedByUserId = currentUserID,
            DueDate = createTaskDto.DueDate,
            CreatedAt = DateTime.UtcNow
        };

        await _mySqlDbContext.Tasks.AddAsync(task);
        await _mySqlDbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<GetTaskResponseDto>> GetTaskAsync(Guid currentUserID, CancellationToken cancellationToken)
    {
        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted, cancellationToken);
        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or inactive.");
        }

        bool isAdmin = currentUser.RoleId == UserRole.Admin.ToId();
        bool isManager = currentUser.RoleId == UserRole.Manager.ToId();
        bool isDevloper = currentUser.RoleId == UserRole.Developer.ToId();


        var query = _mySqlDbContext.Tasks.AsNoTracking().Where(t => _mySqlDbContext.Projects.Any(p => p.ProjectId == t.ProjectId && !p.IsDeleted));


        if (isManager)
        {
            query = query.Where(t => _mySqlDbContext.Projects.Any(p => p.ProjectId == t.ProjectId && p.AssignedToUserId == currentUserID));
        }
        else if (isDevloper)
        {
            query = query.Where(t => t.AssignedToUserId == currentUserID);
        }
        else if (!isAdmin)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "Access denied.");
        }
        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new GetTaskResponseDto
            {
                TaskID = t.TaskId,
                ProjectID = t.ProjectId,
                ProjectName = _mySqlDbContext.Projects.Where(p => p.ProjectId == t.ProjectId).Select(p => p.ProjectName).FirstOrDefault() ?? string.Empty,
                Title = t.Title,
                TaskSummary = t.TaskSummery,
                Status = t.Status,
                Priority = t.Priority,
                AssignedToUserId = t.AssignedToUserId,
                AssignedToName = t.AssignedToUserId == null ? null : _mySqlDbContext.Users.Where(u => u.UserID == t.AssignedToUserId).Select(u => u.FullName).FirstOrDefault(),
                CreatedByUserId = t.CreatedByUserId,
                CreatedByName = _mySqlDbContext.Users.Where(u => u.UserID == t.CreatedByUserId).Select(u => u.FullName).FirstOrDefault() ?? string.Empty,
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt,
                GitHubPRNumber = t.GitHubPRNumber,
                GitHubPRStatus = t.GitHubPRStatus,
                GitHubPRUrl = t.GitHubPRUrl
            })
            .ToListAsync(cancellationToken);

    }

    public async Task<bool> EditTaskAsync(Guid taskID, EditTaskDto editTaskDto, Guid currentUserID, CancellationToken cancellationToken)
    {
        var task = await _mySqlDbContext.Tasks.FirstOrDefaultAsync(t => t.TaskId == taskID, cancellationToken);
        if (task == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Task not found.");
        }
        var project = await _mySqlDbContext.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == task.ProjectId && !p.IsDeleted, cancellationToken);

        if (project == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Project not found.");
        }

        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted, cancellationToken);
        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or inactive.");
        }

        bool isAdmin = currentUser.RoleId == UserRole.Admin.ToId();
        if (!isAdmin && project.AssignedToUserId != currentUserID)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "You can only edit tasks from your assigned projects.");
        }
        task.Title = editTaskDto.Title.Trim();
        task.TaskSummery = editTaskDto.Description?.Trim();
        task.Priority = editTaskDto.Priority.ToString();
        task.DueDate = editTaskDto.DueDate;
        task.UpdatedAt = DateTime.UtcNow;

        await _mySqlDbContext.SaveChangesAsync(cancellationToken);

        return true;
    }


    public async Task<bool> AssignTaskAsync(AssignTaskDto assignTaskDto, Guid currentUserID, CancellationToken cancellationToken)
    {

        var task = await _mySqlDbContext.Tasks.FirstOrDefaultAsync(t => t.TaskId == assignTaskDto.TaskID, cancellationToken);

        if (task == null)
            throw new AppException(StatusCodes.Status404NotFound, "Task not found.");


        var project = await _mySqlDbContext.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == task.ProjectId && !p.IsDeleted, cancellationToken);

        if (project == null)
            throw new AppException(StatusCodes.Status404NotFound, "Project not found.");


        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted, cancellationToken);

        if (currentUser == null)
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or inactive.");

        bool isAdmin = currentUser.RoleId == UserRole.Admin.ToId();


        if (!isAdmin && project.AssignedToUserId != currentUserID)
            throw new AppException(StatusCodes.Status403Forbidden, "This project is not assigned to you.");


        var developer = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == assignTaskDto.DeveloperID && u.RoleId == UserRole.Developer.ToId() && u.IsActive && !u.IsDeleted, cancellationToken);

        if (developer == null)
            throw new AppException(StatusCodes.Status400BadRequest, "Invalid or inactive developer.");

        if (developer.ManagerId != project.AssignedToUserId)
            throw new AppException(StatusCodes.Status400BadRequest, "This developer is not under the project's manager.");

        task.AssignedToUserId = developer.UserID;
        task.UpdatedAt = DateTime.UtcNow;

        await _mySqlDbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
    public async Task<bool> UpdateTaskStatusAsync(Guid taskID, UpdateTaskStatusDto dto, Guid currentUserID, CancellationToken cancellationToken)
    {

        var task = await _mySqlDbContext.Tasks.FirstOrDefaultAsync(t => t.TaskId == taskID, cancellationToken);

        if (task == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Task not found.");
        }


        var project = await _mySqlDbContext.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == task.ProjectId && !p.IsDeleted, cancellationToken);

        if (project == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Project not found.");
        }


        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted, cancellationToken);

        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or inactive.");
        }
        bool isAdmin = currentUser.RoleId == UserRole.Admin.ToId();
        bool isManager = currentUser.RoleId == UserRole.Manager.ToId();
        bool isDeveloper = currentUser.RoleId == UserRole.Developer.ToId();

        if (!isAdmin && !(isManager && project.AssignedToUserId == currentUserID) && !(isDeveloper && task.AssignedToUserId == currentUserID))
        {
            throw new AppException(StatusCodes.Status403Forbidden, "You cannot update this task status.");
        }

        task.Status = dto.Status.ToString();
        task.UpdatedAt = DateTime.UtcNow;

        await _mySqlDbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
