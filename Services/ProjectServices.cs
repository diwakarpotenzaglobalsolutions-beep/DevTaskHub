using System;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Entities;
using DevTaskHubBackend.Exceptions;
using DevTaskHubBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace DevTaskHubBackend.Services;

public interface IProjectServices
{
    Task<CreateProjectResponseDto> CreateProjectAsync(Guid userId, CreateProjectDto createProjectDto, CancellationToken cancellationToken = default);
    Task<ProjectListDto> GetProjectAsync(Guid currentUserId, int page, int pageSize, string? search, string? status, string? sortBy, string? sortOrder, CancellationToken cancellationToken = default);
    Task<GetProjectResponseDto> EditProjectAsync(Guid projectID, EditProjectDto editProjectDto, Guid currentUserID, CancellationToken cancellationToken = default);
    Task<bool> DeleteProjectAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<bool> ReAsignToProjectAsync(ReAsignProjectDto reAsignProjectDto, CancellationToken cancellationToken = default);
    Task<bool> AssignDeveloperToProject(AssignDevelopersDto reAsignProjectDto, Guid currentUserID, CancellationToken cancellationToken = default);
    Task<List<ProjectDevloperDto>> GetProjectDevelopersAsync(Guid projectID, Guid currentUserID, CancellationToken cancellationToken = default);
}

public class ProjectServices : IProjectServices
{

    private readonly MySqlDbContext _myDbContext;

    public ProjectServices(MySqlDbContext myDbContext)
    {
        _myDbContext = myDbContext;
    }

    public async Task<CreateProjectResponseDto> CreateProjectAsync(Guid userId, CreateProjectDto createProjectDto, CancellationToken cancellationToken)
    {
        var user = await _myDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == userId && u.IsActive && !u.IsDeleted);

        if (user == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User account is not active or does not exist.");
        }
        var project = new Projects
        {
            ProjectId = Guid.CreateVersion7(),
            ProjectName = createProjectDto.ProjectName,
            Description = createProjectDto.Description,
            CreatedByUserId = userId,
            AssignedToUserId = userId,
            Status = ProjectStatus.Active.ToString(),
            CreatedAt = DateTime.UtcNow,
        };

        await _myDbContext.Projects.AddAsync(project);
        await _myDbContext.SaveChangesAsync(cancellationToken);

        return new CreateProjectResponseDto
        {
            ProjectId = project.ProjectId,
            ProjectName = project.ProjectName,
            Description = project.Description,
            CreteByUserId = project.CreatedByUserId,
            Status = project.Status,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }


    public async Task<ProjectListDto> GetProjectAsync(Guid currentUserId, int page, int pageSize, string? search, string? status, string? sortBy, string? sortOrder, CancellationToken cancellationToken)
    {
        if (page < 1)
        {
            page = 1;
        }
        if (pageSize < 1)
        {
            pageSize = 10;
        }
        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var currentUser = await _myDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserId && u.IsActive && !u.IsDeleted, cancellationToken);
        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User account is inactive or does not exist.");
        }
        var query = _myDbContext.Projects.AsNoTracking().AsQueryable();
        if (currentUser.RoleId == UserRole.Manager.ToId())
        {
            query = query.Where(p => p.AssignedToUserId == currentUserId);
        }
        else if (currentUser.RoleId == UserRole.Developer.ToId())
        {
            query = query.Where(p => _myDbContext.ProjectMembers.Any(member => member.ProjectId == p.ProjectId && member.UserId == currentUserId));
        }
        else if (currentUser.RoleId != UserRole.Admin.ToId())
        {
            throw new AppException(StatusCodes.Status403Forbidden, "You do not have permission to view all projects.");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(project => project.ProjectName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ProjectStatus>(status.Trim(), true, out var projectStatus) || !Enum.IsDefined(projectStatus))
            {
                throw new AppException(StatusCodes.Status400BadRequest, "Invalid project status.");
            }

            var statusName = projectStatus.ToString();
            query = query.Where(project => project.Status == statusName);
        }

        var projects = from project in query
                       join creator in _myDbContext.Users.AsNoTracking() on project.CreatedByUserId equals creator.UserID
                       join assignee in _myDbContext.Users.AsNoTracking() on project.AssignedToUserId equals assignee.UserID
                       select new GetProjectResponseDto
                       {
                           ProjectId = project.ProjectId,
                           ProjectName = project.ProjectName,
                           Description = project.Description,
                           Status = project.Status,
                           CreatedByUserId = project.CreatedByUserId,
                           CreatedByName = creator.FullName,
                           AssignedToUserId = project.AssignedToUserId,
                           AssignedToName = assignee.FullName,
                           CreatedAt = project.CreatedAt,
                           UpdatedAt = project.UpdatedAt
                       };

        var descending = !string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);
        projects = sortBy?.Trim().ToLowerInvariant() switch
        {
            "name" or "projectname" => descending ? projects.OrderByDescending(project => project.ProjectName) : projects.OrderBy(project => project.ProjectName),
            "status" => descending ? projects.OrderByDescending(project => project.Status) : projects.OrderBy(project => project.Status),
            _ => descending ? projects.OrderByDescending(project => project.CreatedAt) : projects.OrderBy(project => project.CreatedAt)
        };

        var totalCount = await projects.CountAsync(cancellationToken);
        var items = await projects.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new ProjectListDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<GetProjectResponseDto> EditProjectAsync(Guid projectID, EditProjectDto editProjectDto, Guid currentUserID, CancellationToken cancellationToken)
    {
        var currentUser = await _myDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted, cancellationToken);
        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User account is inactive or does not exist.");
        }

        var project = await _myDbContext.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectID, cancellationToken);
        if (project == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Project not found.");
        }
        bool isAdmin = currentUser.RoleId == UserRole.Admin.ToId();
        bool isManager = currentUser.RoleId == UserRole.Manager.ToId();
        if (!isAdmin && (!isManager || project.AssignedToUserId != currentUserID))
        {
            throw new AppException(StatusCodes.Status403Forbidden, "You can only edit projects assigned to you.");
        }
        if (!Enum.IsDefined(editProjectDto.Status))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Invalid project status.");
        }
        project.ProjectName = editProjectDto.ProjectName.Trim();
        project.Description = editProjectDto.Description.Trim();
        project.Status = editProjectDto.Status.ToString();
        project.UpdatedAt = DateTime.UtcNow;

        await _myDbContext.SaveChangesAsync(cancellationToken);

        return new GetProjectResponseDto
        {
            ProjectId = project.ProjectId,
            ProjectName = project.ProjectName,
            Description = project.Description,
            Status = project.Status,
            CreatedByUserId = project.CreatedByUserId,
            AssignedToUserId = project.AssignedToUserId,
            UpdatedAt = project.UpdatedAt
        };

    }

    public async Task<bool> DeleteProjectAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default)
    {

        var currentUser = await _myDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserId && u.IsActive && !u.IsDeleted, cancellationToken);

        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User account is inactive or does not exist.");
        }

        var project = await _myDbContext.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId && !p.IsDeleted, cancellationToken);

        if (project == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Project not found.");
        }

        bool isAdmin = currentUser.RoleId == UserRole.Admin.ToId();
        bool isManager = currentUser.RoleId == UserRole.Manager.ToId();
        if (!isAdmin && (!isManager || project.AssignedToUserId != currentUserId))
        {
            throw new AppException(StatusCodes.Status403Forbidden, "You can only delete projects assigned to you.");
        }
        project.IsDeleted = true;
        project.UpdatedAt = DateTime.UtcNow;
        await _myDbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ReAsignToProjectAsync(ReAsignProjectDto reAsignProjectDto, CancellationToken cancellationToken)
    {
        var project = await _myDbContext.Projects.FirstOrDefaultAsync(p => p.ProjectId == reAsignProjectDto.ProjectID && !p.IsDeleted, cancellationToken);

        if (project == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Project not found");
        }
        var newOwner = await _myDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == reAsignProjectDto.AssignedToUserID && u.IsActive && !u.IsDeleted, cancellationToken);
        if (newOwner == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Selected user not found or inactive.");
        }
        if (newOwner.RoleId != UserRole.Admin.ToId() && newOwner.RoleId != UserRole.Manager.ToId())
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Project can only be assigned to Admin or Project Manager.");
        }
        if (project.AssignedToUserId == reAsignProjectDto.AssignedToUserID)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Project is already assigned to this user.");
        }
        project.AssignedToUserId = reAsignProjectDto.AssignedToUserID;
        project.UpdatedAt = DateTime.UtcNow;

        await _myDbContext.SaveChangesAsync(cancellationToken);
        return true;
    }





public async Task<bool> AssignDeveloperToProject(AssignDevelopersDto assignDevelopersDto,Guid currentUserID,CancellationToken cancellationToken)
{
    var project = await _myDbContext.Projects.FirstOrDefaultAsync(p =>p.ProjectId == assignDevelopersDto.ProjectID && !p.IsDeleted,cancellationToken);

    if (project == null)
        throw new AppException(StatusCodes.Status404NotFound, "Project not found.");

    var currentUser = await _myDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted, cancellationToken);

    if (currentUser == null)
        throw new AppException(StatusCodes.Status403Forbidden, "User account is inactive or not found.");

    bool isAdmin = currentUser.RoleId == UserRole.Admin.ToId();

    if (!isAdmin && project.AssignedToUserId != currentUserID)
        throw new AppException(StatusCodes.Status403Forbidden, "This project is not assigned to you.");

    var developerIds = assignDevelopersDto.DeveloperIDs.Distinct().ToList();

    var developers = await _myDbContext.Users.AsNoTracking().Where(u => developerIds.Contains(u.UserID) &&u.RoleId == UserRole.Developer.ToId() &&u.IsActive && !u.IsDeleted)
        .ToListAsync(cancellationToken);

    if (developers.Count != developerIds.Count)
        throw new AppException(StatusCodes.Status400BadRequest, "One or more selected developers are invalid.");

    var existingMembers = await _myDbContext.ProjectMembers
        .Where(pm => pm.ProjectId == project.ProjectId)
        .ToListAsync(cancellationToken);

    var existingDeveloperIds = existingMembers.Select(pm => pm.UserId).ToHashSet();
    if (developers.Any(d => d.ManagerId != project.AssignedToUserId && !existingDeveloperIds.Contains(d.UserID)))
        throw new AppException(StatusCodes.Status403Forbidden, "You can only assign developers under this project's manager.");

    var membersToRemove = existingMembers.Where(pm => !developerIds.Contains(pm.UserId)).ToList();

    _myDbContext.ProjectMembers.RemoveRange(membersToRemove);

    foreach (var developer in developers)
    {
        if (!existingDeveloperIds.Contains(developer.UserID))
        {
            _myDbContext.ProjectMembers.Add(new ProjectMember
            {
                ProjectMemberId = Guid.CreateVersion7(),
                ProjectId = project.ProjectId,
                UserId = developer.UserID
            });
        }
    }

    await _myDbContext.SaveChangesAsync(cancellationToken);

    return true;
}

    public async Task<List<ProjectDevloperDto>> GetProjectDevelopersAsync(Guid projectID, Guid currentUserID, CancellationToken cancellationToken)
    {
        var project = await _myDbContext.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == projectID && !p.IsDeleted, cancellationToken);

        if (project == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Project not found.");
        }

        var currentUser = await _myDbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted, cancellationToken);

        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or inactive.");
        }

        bool isAdmin = currentUser.RoleId == UserRole.Admin.ToId();
        bool isManager = currentUser.RoleId == UserRole.Manager.ToId();
        bool isDeveloper = currentUser.RoleId == UserRole.Developer.ToId();

      
        bool isProjectMember = false;

        if (isDeveloper)
        {
            isProjectMember = await _myDbContext.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectID && pm.UserId == currentUserID, cancellationToken);
        }

     
        if (!isAdmin && !(isManager && project.AssignedToUserId == currentUserID) && !(isDeveloper && isProjectMember))
        {
            throw new AppException(StatusCodes.Status403Forbidden, "You do not have access to this project.");
        }

        return await _myDbContext.ProjectMembers.AsNoTracking()
            .Where(member => member.ProjectId == projectID).Join(
                _myDbContext.Users,
                member => member.UserId,
                user => user.UserID,
                (member, user) => user)
            .Where(user => user.RoleId == UserRole.Developer.ToId() && user.IsActive && !user.IsDeleted)
            .OrderBy(user => user.FullName)
            .Select(user => new ProjectDevloperDto
            {
                UserId = user.UserID,
                FullName = user.FullName,
                GitHubAvatarUrl = user.GitHubAvatarUrl
            })
            .ToListAsync(cancellationToken);
    }
}
