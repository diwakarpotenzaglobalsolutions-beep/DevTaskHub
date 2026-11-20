using System;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Entities;
using DevTaskHubBackend.Exceptions;
using DevTaskHubBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace DevTaskHubBackend.Services;

public interface ILeaveServices
{
    Task<bool> CreateLeaveTypeAsync(Guid currendUserID, CreateLeaveTypeDto createLeaveTypeDto, CancellationToken cancellationToken = default);
    Task<List<GetLeaveTypeDto>> GetAllLeaveTypesAsync(Guid currentUserID, CancellationToken cancellationToken = default);
    Task<bool> UpdateLeaveTypeAsync(Guid currendUserID, CreateLeaveTypeDto createLeaveTypeDto, Guid leaveTypeId, CancellationToken cancellationToken = default);
    Task<bool> DeleteLeaveTypeAsync(Guid currentUserID, Guid leaveTypeId, CancellationToken cancellationToken = default);
}

public class LeaveServices : ILeaveServices
{
    private readonly MySqlDbContext _mySqlDbContext;

    public LeaveServices(MySqlDbContext mySqlDbContext)
    {
        _mySqlDbContext = mySqlDbContext;
    }

    public async Task<bool> CreateLeaveTypeAsync(Guid currendUserID, CreateLeaveTypeDto createLeaveTypeDto, CancellationToken cancellationToken)
    {
        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currendUserID && u.IsActive && !u.IsDeleted, cancellationToken);
        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or account is inactive/deleted.");
        }
        var alreadyExists = await _mySqlDbContext.LeaveTypes.AsNoTracking().AnyAsync(x => x.Name == createLeaveTypeDto.Name, cancellationToken);
        if (alreadyExists)
        {
            throw new AppException(StatusCodes.Status409Conflict, "Leave type already exists.");
        }

        var leaveType = new LeaveTypes
        {
            LeaveTypeId = Guid.CreateVersion7(),
            Name = createLeaveTypeDto.Name,
            Description = createLeaveTypeDto.Description.Trim(),
            AnnualQuota = createLeaveTypeDto.AnnualQuota,
            IsPaid = createLeaveTypeDto.IsPaid,
            AllowHalfDay = createLeaveTypeDto.IsHalfDay,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _mySqlDbContext.LeaveTypes.Add(leaveType);

        var result = await _mySqlDbContext.SaveChangesAsync(cancellationToken);

        return true;
    }


    public async Task<List<GetLeaveTypeDto>> GetAllLeaveTypesAsync(Guid currentUserID, CancellationToken cancellationToken)
    {
        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted, cancellationToken);
        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or account is inactive/deleted.");
        }

        var leaveType = await _mySqlDbContext.LeaveTypes.AsNoTracking().OrderBy(x => x.Name).Select(x => new
            GetLeaveTypeDto
        {
            LeaveTypeId = x.LeaveTypeId,
            Name = x.Name,
            Description = x.Description,
            AnnualQuota = x.AnnualQuota,
            IsPaid = x.IsPaid,
            AllowHalfDay = x.AllowHalfDay,
            IsActive = x.IsActive
        }
        ).ToListAsync(cancellationToken);

        return leaveType;
    }


    public async Task<bool> UpdateLeaveTypeAsync(Guid currendUserID, CreateLeaveTypeDto createLeaveTypeDto, Guid leaveTypeId, CancellationToken cancellationToken)
    {
        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currendUserID && u.IsActive && !u.IsDeleted, cancellationToken);
        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or account is inactive/deleted.");
        }


        var leaveType = await _mySqlDbContext.LeaveTypes.FirstOrDefaultAsync(l => l.LeaveTypeId == leaveTypeId && l.IsActive, cancellationToken);

        if (leaveType == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Leave type not found.");
        }
        leaveType.Name = createLeaveTypeDto.Name.Trim();
        leaveType.Description = createLeaveTypeDto.Description.Trim();
        leaveType.AnnualQuota = createLeaveTypeDto.AnnualQuota;
        leaveType.IsPaid = createLeaveTypeDto.IsPaid;
        leaveType.AllowHalfDay = createLeaveTypeDto.IsHalfDay;
        leaveType.UpdatedAt = DateTime.UtcNow;

        await _mySqlDbContext.SaveChangesAsync(cancellationToken);

        return true;
    }


    public async Task<bool> DeleteLeaveTypeAsync(Guid currentUserID, Guid leaveTypeId, CancellationToken cancellationToken)
    {
        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted, cancellationToken);

        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or account is inactive/deleted.");
        }

        var leaveType = await _mySqlDbContext.LeaveTypes.FirstOrDefaultAsync(l => l.LeaveTypeId == leaveTypeId, cancellationToken);

        if (leaveType == null)
        {

            throw new AppException(StatusCodes.Status404NotFound, "Leave type not found.");
        }

        leaveType.IsActive = false;
        leaveType.UpdatedAt = DateTime.UtcNow;

        await _mySqlDbContext.SaveChangesAsync(cancellationToken);

        return true;
    }


    public async Task<bool> AllocateLeaveBalanceAsync(Guid currentUserID, AllocateLeaveBalanceDto allocateLeaveBalanceDto, CancellationToken cancellationToken)
    {
        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted);
        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or account is inactive/deleted.");
        }

        var employeeExists = await _mySqlDbContext.Users.AsNoTracking().AnyAsync(e => e.UserID == allocateLeaveBalanceDto.UserId && e.IsActive && !e.IsDeleted);

        if (!employeeExists)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Employee not found or inactive.");
        }

        var leaveType = await _mySqlDbContext.LeaveTypes.AsNoTracking().FirstOrDefaultAsync(l => l.LeaveTypeId == allocateLeaveBalanceDto.LeaveTypeId && l.IsActive, cancellationToken);
        if (leaveType == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Active leave type not found.");
        }
        var alreadyAllocated = await _mySqlDbContext.EmployeeLeaveBalances.AsNoTracking().AnyAsync(x => x.UserId == allocateLeaveBalanceDto.UserId && x.LeaveTypeId == allocateLeaveBalanceDto.LeaveTypeId && x.Year == allocateLeaveBalanceDto.Year, cancellationToken);
        if (alreadyAllocated)
        {
            throw new AppException(StatusCodes.Status409Conflict, "Leave balance already allocated for this employee, leave type and year.");
        }
        var balance = new EmployeeLeaveBalances
        {
            EmployeeLeaveBalanceId = Guid.CreateVersion7(),
            UserId = allocateLeaveBalanceDto.UserId,
            LeaveTypeId = allocateLeaveBalanceDto.LeaveTypeId,
            Year = allocateLeaveBalanceDto.Year,
            AllocatedDays = allocateLeaveBalanceDto.AllocatedDays,
            UsedDays = 0,
            CreatedAt = DateTime.UtcNow
        };

        _mySqlDbContext.EmployeeLeaveBalances.Add(balance);

        await _mySqlDbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

}



