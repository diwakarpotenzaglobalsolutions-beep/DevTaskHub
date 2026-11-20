using System;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Entities;
using DevTaskHubBackend.Exceptions;
using DevTaskHubBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace DevTaskHubBackend.Services;

public interface ILeaveApplicationServices
{
    Task<List<AvailableLeaveTypeDto>> GetAvailableLeaveTypesAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<bool> ApplyLeaveAsync(Guid currentUserID, ApplyLeaveDto applyLeaveDto, CancellationToken cancellationToken=default);
    Task<List<MyLeaveDto>> GetMyLeavesAsync(Guid currentUserId,CancellationToken cancellationToken=default);
}

public class LeaveApplicationServices : ILeaveApplicationServices
{
    private readonly MySqlDbContext _mySqlDbContext;

    public LeaveApplicationServices(MySqlDbContext mySqlDbContext)
    {
        _mySqlDbContext = mySqlDbContext;
    }


    public async Task<List<AvailableLeaveTypeDto>> GetAvailableLeaveTypesAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var activeUser = await _mySqlDbContext.Users.AsNoTracking()
            .AnyAsync(user => user.UserID == currentUserId && user.IsActive && !user.IsDeleted, cancellationToken);
        if (!activeUser)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or account is inactive/deleted.");
        }

        return await _mySqlDbContext.LeaveTypes.AsNoTracking()
            .Where(leaveType => leaveType.IsActive)
            .OrderBy(leaveType => leaveType.Name)
            .Select(leaveType => new AvailableLeaveTypeDto(leaveType.LeaveTypeId, leaveType.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ApplyLeaveAsync(Guid currentUserID, ApplyLeaveDto applyLeaveDto, CancellationToken cancellationToken)
    {
        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted, cancellationToken);
        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or account is inactive/deleted.");
        }

        if (applyLeaveDto.EndDate < applyLeaveDto.StartDate)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "End date cannot be before start date.");
        }
        var indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        var indiaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indiaTimeZone);

        var today = DateOnly.FromDateTime(indiaTime);

        if (applyLeaveDto.StartDate < today)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Past date leave is not allowed.");
        }
        if (applyLeaveDto.StartDate.Year != applyLeaveDto.EndDate.Year)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Cross-year leave is not supported yet.");
        }
        var leaveType = await _mySqlDbContext.LeaveTypes.AsTracking().FirstOrDefaultAsync(x => x.LeaveTypeId == applyLeaveDto.LeaveTypeId && x.IsActive, cancellationToken);
        if (leaveType == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Active leave type not found.");
        }
        decimal totalDays = applyLeaveDto.EndDate.DayNumber - applyLeaveDto.StartDate.DayNumber + 1;
        var overlappingLeave = await _mySqlDbContext.LeaveRequests.AsNoTracking().AnyAsync(x => x.UserId == currentUserID && (x.Status == LeaveStatus.Pending.ToString() || x.Status == LeaveStatus.Approved.ToString()) && x.StartDate <= applyLeaveDto.EndDate && x.EndDate >= applyLeaveDto.StartDate);
        if (overlappingLeave)
        {
            throw new AppException(StatusCodes.Status409Conflict, "You already have a leave request for these dates.");
        }
        if (leaveType.IsPaid)
        {
            var balance = await _mySqlDbContext.EmployeeLeaveBalances.AsNoTracking().FirstOrDefaultAsync(
                    x => x.UserId == currentUserID && x.LeaveTypeId == applyLeaveDto.LeaveTypeId && x.Year == applyLeaveDto.StartDate.Year, cancellationToken);

            if (balance == null)
            {
                throw new AppException(StatusCodes.Status400BadRequest, "Leave balance has not been allocated.");
            }


            var pendingDays = await _mySqlDbContext.LeaveRequests.AsNoTracking().Where(x => x.UserId == currentUserID && x.LeaveTypeId == applyLeaveDto.LeaveTypeId && x.Status == LeaveStatus.Pending.ToString() && x.StartDate.Year == applyLeaveDto.StartDate.Year).SumAsync(x => (decimal?)x.TotalDays, cancellationToken) ?? 0m;

            decimal availableDays = balance.AllocatedDays - balance.UsedDays - pendingDays;

            if (totalDays > availableDays)
            {
                throw new AppException(StatusCodes.Status400BadRequest, $"Insufficient leave balance. Available: {availableDays} days.");
            }
        }

        var leaveRequest = new LeaveRequests
        {
            LeaveRequestId = Guid.CreateVersion7(),
            UserId = currentUserID,
            LeaveTypeId = applyLeaveDto.LeaveTypeId,
            StartDate = applyLeaveDto.StartDate,
            EndDate = applyLeaveDto.EndDate,
            TotalDays = totalDays,
            Reason = applyLeaveDto.Reason.Trim(),
            Status = LeaveStatus.Pending.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _mySqlDbContext.LeaveRequests.Add(leaveRequest);

        await _mySqlDbContext.SaveChangesAsync(cancellationToken);

        return true;
    }


    public async Task<List<MyLeaveDto>> GetMyLeavesAsync(Guid currentUserId,CancellationToken cancellationToken)
{
    var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserId &&u.IsActive &&!u.IsDeleted,cancellationToken);

    if (currentUser == null)
    {
        throw new AppException(StatusCodes.Status403Forbidden,"User not found or account is inactive/deleted.");
    }

    var leaveList = await (
        from leave in _mySqlDbContext.LeaveRequests.AsNoTracking()

        join leaveType in _mySqlDbContext.LeaveTypes.AsNoTracking()
            on leave.LeaveTypeId equals leaveType.LeaveTypeId

        where leave.UserId == currentUserId

        orderby leave.CreatedAt descending

        select new MyLeaveDto
        {
            LeaveRequestId = leave.LeaveRequestId,

            LeaveTypeId = leave.LeaveTypeId,

            LeaveTypeName = leaveType.Name,

            StartDate = leave.StartDate,

            EndDate = leave.EndDate,

            TotalDays = leave.TotalDays,

            Reason = leave.Reason,

            Status = leave.Status,

            RejectionReason = leave.RejectionReason,

            ReviewedAt = leave.ReviewedAt,

            CreatedAt = leave.CreatedAt
        }
    ).ToListAsync(cancellationToken);

    return leaveList;
}

}
