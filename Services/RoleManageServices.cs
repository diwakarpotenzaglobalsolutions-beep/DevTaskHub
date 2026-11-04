using System;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Exceptions;
using DevTaskHubBackend.Helpers;
using DevTaskHubBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace DevTaskHubBackend.Services;

public interface IRoleManageServices
{
    Task<bool> AssignRoleAsync(RoleAssignDto roleAssignDto, CancellationToken cancellationToken = default);
    Task<List<ProfileResponseDto>> GetAllUserAsync(CancellationToken cancellationToken = default);
    Task<List<RoleListDto>> GetAllRoleListAsync(CancellationToken cancellationToken=default);
}

public class RoleManageServices : IRoleManageServices
{
    private readonly MySqlDbContext _context;

    public RoleManageServices(MySqlDbContext mySqlDbContext)
    {
        _context = mySqlDbContext;
    }


    public async Task<List<ProfileResponseDto>> GetAllUserAsync(CancellationToken cancellationToken)
    {
        var users = await _context.Users.AsNoTracking().Where(u => !u.IsDeleted).Select(u => new
        {
            u.UserID,
            u.FullName,
            u.Email,
            u.PhoneNumber,
            u.RoleId,
            u.IsActive
        }).ToListAsync(cancellationToken);

        return users.Select(user => new ProfileResponseDto
        {
            UserId = user.UserID,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = RoleHelper.GetRoleName(user.RoleId),
            IsActive = user.IsActive
        }).ToList();
    }

    public async Task<bool> AssignRoleAsync(RoleAssignDto roleAssignDto, CancellationToken cancellationToken)
    {

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == roleAssignDto.UserID && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "User not found.");
        }

        var validRoleIds = new[]
        {
        UserRole.Admin.ToId(),
        UserRole.Manager.ToId(),
        UserRole.Developer.ToId()
    };

        if (!validRoleIds.Contains(roleAssignDto.RoleID))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Invalid role selected.");
        }


        if (user.RoleId == roleAssignDto.RoleID)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "This role is already assigned to the user.");
        }

        user.RoleId = roleAssignDto.RoleID;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

 public async Task<List<RoleListDto>> GetAllRoleListAsync(
    CancellationToken cancellationToken)
{
    return await _context.Roles
        .AsNoTracking()
        .OrderBy(r => r.RoleName)
        .Select(role => new RoleListDto
        {
            RoleID = role.RoleId,
            RoleName = role.RoleName
        })
        .ToListAsync(cancellationToken);
}
}
