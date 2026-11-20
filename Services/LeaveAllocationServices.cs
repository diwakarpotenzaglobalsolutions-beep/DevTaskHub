using System;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Entities;
using DevTaskHubBackend.Exceptions;
using DevTaskHubBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace DevTaskHubBackend.Services;

public interface ILeaveAllocationServices
{
    Task<bool> AllocateLeaveBalanceAsync(Guid currentUserID, AllocateLeaveBalanceDto allocateLeaveBalanceDto, CancellationToken cancellationToken = default);
    Task<List<EmployeeLeaveBalanceDto>> GetEmployeeBalanceAsync(Guid currentUserID, Guid userID, int? year, CancellationToken cancellationToken = default);
    Task<List<EmployeeLeaveBalanceDto>> GetMyLeaveBalanceAsync(Guid currentUserId,int? year,CancellationToken cancellationToken=default);
}

public class LeaveAllocationServices : ILeaveAllocationServices
{
    private readonly MySqlDbContext _mySqlDbContext;

    public LeaveAllocationServices(MySqlDbContext mySqlDbContext)
    {
        _mySqlDbContext = mySqlDbContext;
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

    public async Task<List<EmployeeLeaveBalanceDto>> GetEmployeeBalanceAsync(Guid currentUserID, Guid userID, int? year, CancellationToken cancellationToken)
    {
        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted, cancellationToken);
        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or account is inactive/deleted.");
        }
        if (userID == Guid.Empty)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Invalid employee ID.");
        }
        var employeeExists = await _mySqlDbContext.Users.AsNoTracking().AnyAsync(u => u.UserID == userID && u.IsActive && !u.IsDeleted, cancellationToken);

        if (!employeeExists)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Employee not found or inactive.");
        }
        var indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        var indiaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indiaTimeZone);

        int selectedYear = year ?? indiaTime.Year;
        if (selectedYear < 2000 || selectedYear > 2100)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Invalid leave year.");
        }

        var balances = await (
        from balance in _mySqlDbContext.EmployeeLeaveBalances.AsNoTracking()

        join leaveType in _mySqlDbContext.LeaveTypes.AsNoTracking() on balance.LeaveTypeId equals leaveType.LeaveTypeId

        where balance.UserId == userID && balance.Year == selectedYear

        orderby leaveType.Name

        select new EmployeeLeaveBalanceDto
        {
            UserId = balance.UserId,
            LeaveTypeId = balance.LeaveTypeId,
            LeaveTypeName = leaveType.Name,
            Year = balance.Year,
            AllocatedDays = balance.AllocatedDays,
            UsedDays = balance.UsedDays,
            PendingDays = 0,
            RemainingDays = balance.AllocatedDays - balance.UsedDays
        }
    ).ToListAsync(cancellationToken);
        var startDate = new DateOnly(selectedYear, 1, 1);
        var endDate = new DateOnly(selectedYear + 1, 1, 1);

        var pendingRequests = await _mySqlDbContext.LeaveRequests.AsNoTracking().Where(x => x.UserId == userID && x.Status == LeaveStatus.Pending.ToString() && x.StartDate >= startDate && x.EndDate < endDate)
            .GroupBy(x => x.LeaveTypeId)
            .Select(x => new
            {
                LeaveTypeId = x.Key,
                PendingDays = x.Sum(r => r.TotalDays)
            })
            .ToListAsync(cancellationToken);

        var pendingLookup = pendingRequests.ToDictionary(x => x.LeaveTypeId, x => x.PendingDays);

        foreach (var balance in balances)
        {
            balance.PendingDays = pendingLookup.GetValueOrDefault(balance.LeaveTypeId, 0);

            balance.RemainingDays = balance.AllocatedDays - balance.UsedDays - balance.PendingDays;
        }

        return balances;

    }

    public async Task<List<EmployeeLeaveBalanceDto>> GetMyLeaveBalanceAsync(Guid currentUserID, int? year, CancellationToken cancellationToken)
    {
        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted, cancellationToken);
        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or account is inactive/deleted.");
        }
        var indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        var indiaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indiaTimeZone);

        int selectedYear = year ?? indiaTime.Year;
        if (selectedYear < 2000 || selectedYear > 2100)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Invalid leave year.");
        }
        var balances = await (
            from balance in _mySqlDbContext.EmployeeLeaveBalances.AsNoTracking()
            join leaveType in _mySqlDbContext.LeaveTypes.AsNoTracking() on balance.LeaveTypeId equals leaveType.LeaveTypeId
            where balance.UserId == currentUserID && balance.Year == selectedYear
            orderby leaveType.Name
            select new EmployeeLeaveBalanceDto
            {
                UserId = balance.UserId,
                LeaveTypeId = balance.LeaveTypeId,
                LeaveTypeName = leaveType.Name,
                Year = balance.Year,
                AllocatedDays = balance.AllocatedDays,
                UsedDays = balance.UsedDays,
                PendingDays = 0,
                RemainingDays =
                balance.AllocatedDays - balance.UsedDays
            }
        ).ToListAsync(cancellationToken);

        var startDate = new DateOnly(selectedYear, 1, 1);
        var endDate = new DateOnly(selectedYear, 12, 31);

        var pendingRequests = await _mySqlDbContext.LeaveRequests.AsNoTracking().Where(x =>
           x.UserId == currentUserID &&
           x.Status == LeaveStatus.Pending.ToString() &&
           x.StartDate >= startDate &&
           x.EndDate <= endDate).GroupBy(x => x.LeaveTypeId).Select(x => new
           {
               LeaveTypeId = x.Key,
               PendingDays = x.Sum(r => r.TotalDays)
           }).ToListAsync(cancellationToken);

        var pendingLookup = pendingRequests.ToDictionary(x => x.LeaveTypeId,x => x.PendingDays);

        foreach (var balance in balances)
        {
            balance.PendingDays = pendingLookup.GetValueOrDefault(balance.LeaveTypeId, 0m);

            balance.RemainingDays = balance.AllocatedDays - balance.UsedDays - balance.PendingDays;
        }

        return balances;
    }

}
