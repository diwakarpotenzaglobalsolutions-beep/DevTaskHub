using System;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Entities;
using DevTaskHubBackend.Exceptions;
using DevTaskHubBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace DevTaskHubBackend.Services;

public interface IAttendanceServices
{
    Task<AttendanceDto> AttendanceAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<AttendanceDto> CheckOutAsync(Guid currentUserID, CancellationToken cancellationTokenp = default);
    Task<TodayAttendanceDto> TodayAttendanceAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<List<AttendanceHistoryDto>> GetMyAttendanceAsync(Guid currentUserId, int? month, int? year, CancellationToken cancellationToken = default);
    Task<List<AttendanceHistoryDto>> GetTeamAttendanceAsync(Guid currentUserID, DateOnly? date, CancellationToken cancellationToken = default);
    Task<List<AttendanceHistoryDto>> GetAllAttendanceAsync(Guid currentUserID,DateOnly? date,Guid userID,CancellationToken cancellationToken=default);

    Task<bool> CorrectAttendanceAsync(Guid currentUserID, Guid attendanceID, CorrectAttendanceDto correctAttendanceDto, CancellationToken cancellationToken=default);
}

public class AttendanceServices : IAttendanceServices
{
    private const string AbsentStatus = "Absent";
    private readonly MySqlDbContext _mySqlDbContext;


    public AttendanceServices(MySqlDbContext mySqlDbContext)
    {
        _mySqlDbContext = mySqlDbContext;
    }

    public async Task<AttendanceDto> AttendanceAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserId && u.IsActive && !u.IsDeleted);

        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or account is inactive/deleted.");
        }

        var now = DateTime.UtcNow;
        var indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        var localTime = TimeZoneInfo.ConvertTimeFromUtc(now, indiaTimeZone);
        var today = DateOnly.FromDateTime(localTime);

        var alreadyCheckedIn = await _mySqlDbContext.Attendances.AnyAsync(x => x.UserId == currentUserId && x.AttendanceDate == today);
        if (alreadyCheckedIn)
        {
            throw new AppException(StatusCodes.Status409Conflict, "Already checked in today.");
        }
        var attendance = new Attendance
        {
            AttendanceId = Guid.CreateVersion7(),
            UserId = currentUserId,
            CheckInTime = now,
            AttendanceDate = today,
            WorkingMinutes = 0,
            Status = "Present",
            CreatedAt = now
        };

        await _mySqlDbContext.Attendances.AddAsync(attendance);
        await _mySqlDbContext.SaveChangesAsync(cancellationToken);

        return new AttendanceDto
        {
            AttendanceID = attendance.AttendanceId,
            UserID = attendance.UserId,
            AttendanceDate = attendance.AttendanceDate,
            CheckInTime = attendance.CheckInTime,
            Status = attendance.Status,
        };
    }

    public async Task<AttendanceDto> CheckOutAsync(Guid currentUserID, CancellationToken cancellationToken)
    {
        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted, cancellationToken);

        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or account is inactive/deleted.");
        }
        var now = DateTime.UtcNow;
        var indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(now, indiaTimeZone);

        var today = DateOnly.FromDateTime(localTime);

        var alreadyCheckedIn = await _mySqlDbContext.Attendances.FirstOrDefaultAsync(x => x.UserId == currentUserID && x.AttendanceDate == today, cancellationToken);
        if (alreadyCheckedIn == null || alreadyCheckedIn.CheckInTime == null)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Please check in first.");
        }
        if (alreadyCheckedIn.CheckOutTime != null)
        {
            throw new AppException(StatusCodes.Status409Conflict, "You have already checked out today.");
        }
        alreadyCheckedIn.CheckOutTime = now;
        alreadyCheckedIn.WorkingMinutes = (int)(now - alreadyCheckedIn.CheckInTime.Value).TotalMinutes;
        alreadyCheckedIn.UpdatedAt = now;
        await _mySqlDbContext.SaveChangesAsync(cancellationToken);
        return new AttendanceDto
        {
            AttendanceID = alreadyCheckedIn.AttendanceId,
            UserID = alreadyCheckedIn.UserId,
            AttendanceDate = alreadyCheckedIn.AttendanceDate,
            CheckInTime = alreadyCheckedIn.CheckInTime,
            CheckOutTime = alreadyCheckedIn.CheckOutTime,
            WorkingMinutes = alreadyCheckedIn.WorkingMinutes,
            Status = alreadyCheckedIn.Status
        };

    }


    public async Task<TodayAttendanceDto> TodayAttendanceAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserId && u.IsActive && !u.IsDeleted);
        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or account is inactive/deleted.");
        }

        var now = DateTime.UtcNow;

        var indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        var localTime = TimeZoneInfo.ConvertTimeFromUtc(now, indiaTimeZone);

        var today = DateOnly.FromDateTime(localTime);

        var attendance = await _mySqlDbContext.Attendances.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == currentUserId && x.AttendanceDate == today, cancellationToken);
        if (attendance == null)
        {
            return new TodayAttendanceDto
            {
                AttendanceDate = today,
                IsCheckedIn = false,
                IsCheckedOut = false,
                Status = AbsentStatus,
                WorkingMinutes = 0
            };
        }
        return new TodayAttendanceDto
        {
            AttendanceID = attendance.AttendanceId,
            AttendanceDate = attendance.AttendanceDate,
            CheckInTime = attendance.CheckInTime,
            CheckOutTime = attendance.CheckOutTime,
            IsCheckedIn = attendance.CheckInTime.HasValue,
            IsCheckedOut = attendance.CheckOutTime.HasValue,
            WorkingMinutes = attendance.CheckInTime.HasValue ? attendance.WorkingMinutes : 0,
            Status = attendance.CheckInTime.HasValue ? attendance.Status : AbsentStatus
        };
    }


    public async Task<List<AttendanceHistoryDto>> GetMyAttendanceAsync(Guid currentUserId, int? month, int? year, CancellationToken cancellationToken)
    {
        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserId && u.IsActive && !u.IsDeleted, cancellationToken);
        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or account is inactive/deleted.");
        }
        var now = DateTime.UtcNow;

        var indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        var localTime = TimeZoneInfo.ConvertTimeFromUtc(now, indiaTimeZone);

        int selectedMonth = month ?? localTime.Month;
        int selectedYear = year ?? localTime.Year;
        if (selectedMonth < 1 || selectedMonth > 12)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Invalid month. Month must be between 1 and 12.");
        }

        if (selectedYear < 1 || selectedYear > 9999)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Invalid year.");
        }
        var startDate = new DateOnly(selectedYear, selectedMonth, 1);
        var fullName = currentUser.FullName;

        var endDate = startDate.AddMonths(1);
        var attendanceList = await _mySqlDbContext.Attendances.AsNoTracking().Where(x => x.UserId == currentUserId && x.AttendanceDate >= startDate && x.AttendanceDate < endDate).OrderByDescending(x => x.AttendanceDate)
        .Select(x => new AttendanceHistoryDto
        {
            UserID = currentUserId,
            FullName = fullName,
            AttendanceID = x.AttendanceId,
            AttendanceDate = x.AttendanceDate,
            CheckInTime = x.CheckInTime,
            CheckOutTime = x.CheckOutTime,
            WorkingMinutes = x.WorkingMinutes,
            Status = x.Status,
            Remarks = x.Remarks
        }).ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(localTime);
        if (today >= startDate && today < endDate)
        {
            var todayAttendance = attendanceList.FirstOrDefault(x => x.AttendanceDate == today);
            if (todayAttendance == null)
            {
                attendanceList.Insert(0, new AttendanceHistoryDto
                {
                    UserID = currentUserId,
                    FullName = fullName,
                    AttendanceDate = today,
                    Status = AbsentStatus
                });
            }
            else if (todayAttendance.CheckInTime == null)
            {
                todayAttendance.Status = AbsentStatus;
            }
        }

        return attendanceList;
    }

    public async Task<List<AttendanceHistoryDto>> GetTeamAttendanceAsync(Guid currentUserID, DateOnly? date, CancellationToken cancellationToken)
    {
        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted, cancellationToken);
        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or account is inactive/deleted.");
        }
        if (currentUser.RoleId != UserRole.Manager.ToId())
        {
            throw new AppException(StatusCodes.Status403Forbidden, "Only managers can view team attendance.");
        }
        var indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indiaTimeZone);

        var selectedDate = date ?? DateOnly.FromDateTime(localTime);

        var developers = await _mySqlDbContext.Users.AsNoTracking()
            .Where(developer => developer.ManagerId == currentUserID
                && developer.RoleId == UserRole.Developer.ToId()
                && developer.IsActive
                && !developer.IsDeleted)
            .OrderBy(developer => developer.FullName)
            .Select(developer => new { developer.UserID, developer.FullName })
            .ToListAsync(cancellationToken);

        var attendanceByUser = await AttendanceForDateAsync(selectedDate, cancellationToken);

        return developers.Select(developer =>
            ToAttendanceHistory(developer.UserID, developer.FullName, selectedDate, attendanceByUser.GetValueOrDefault(developer.UserID)))
            .ToList();
    }

public async Task<List<AttendanceHistoryDto>> GetAllAttendanceAsync(Guid currentUserID,DateOnly? date,Guid userID,CancellationToken cancellationToken)
{
    var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u =>u.UserID == currentUserID &&u.IsActive &&!u.IsDeleted,cancellationToken);

    if (currentUser == null)
        throw new AppException(403,"User not found or account is inactive/deleted.");

  
    bool isAdmin = currentUser.RoleId == UserRole.Admin.ToId();
    bool isHR = currentUser.RoleId == UserRole.Hr.ToId();

    if (!isAdmin && !isHR)
        throw new AppException(403,"Only Admin and HR can view all attendance.");

  
    var indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

    var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indiaTimeZone));

   
    var selectedDate = date ?? today;

    if (selectedDate.Year < 2000 || selectedDate > today)throw new AppException(400, "Invalid attendance date.");

  
    var employees = _mySqlDbContext.Users.AsNoTracking()
        .Where(employee => employee.IsActive && !employee.IsDeleted);

    if (userID != Guid.Empty)
    {
        employees = employees.Where(employee => employee.UserID == userID);
    }

    var people = await employees
        .OrderBy(employee => employee.FullName)
        .Select(employee => new { employee.UserID, employee.FullName })
        .ToListAsync(cancellationToken);

    var attendanceByUser = await AttendanceForDateAsync(selectedDate, cancellationToken);

    return people
        .Select(employee => ToAttendanceHistory(employee.UserID, employee.FullName, selectedDate, attendanceByUser.GetValueOrDefault(employee.UserID)))
        .OrderBy(row => row.Status == AbsentStatus ? 0 : 1)
        .ThenBy(row => row.FullName)
        .ToList();
}

    public async Task<bool> CorrectAttendanceAsync(Guid currentUserID, Guid attendanceID, CorrectAttendanceDto correctAttendanceDto, CancellationToken cancellationToken)
    {
        var currentUser = await _mySqlDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserID && u.IsActive && !u.IsDeleted, cancellationToken);
        if (currentUser == null)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "User not found or account is inactive/deleted.");
        }

        bool isAdmin = currentUser.RoleId == UserRole.Admin.ToId();
        bool isHR = currentUser.RoleId == UserRole.Hr.ToId();

        if (!isAdmin && !isHR)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "Only Admin and HR can correct attendance.");
        }
        var checkIn = correctAttendanceDto.CheckInTime;
        var checkOut = correctAttendanceDto.CheckOutTime;

        if (checkIn.Kind != DateTimeKind.Utc || checkOut.Kind != DateTimeKind.Utc)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Check-In and Check-Out times must be in UTC.");
        }
        if (checkOut <= checkIn)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Check-Out time must be after Check-In time.");
        }
        if (checkIn > DateTime.UtcNow || checkOut > DateTime.UtcNow)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Check-In and Check-Out times cannot be in the future.");
        }
        var attendance = await _mySqlDbContext.Attendances.FirstOrDefaultAsync(a => a.AttendanceId == attendanceID, cancellationToken);

        if (attendance == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Attendance record not found.");
        }
        var indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        var localCheckIn = TimeZoneInfo.ConvertTimeFromUtc(checkIn, indiaTimeZone);

        var correctedDate = DateOnly.FromDateTime(localCheckIn);

        var duration = checkOut - checkIn;

        if (duration.TotalMinutes > int.MaxValue)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Attendance duration is too large.");
        }
        var workingMinutes =(int)Math.Floor(duration.TotalMinutes);
        bool hasChanges = attendance.AttendanceDate != correctedDate || attendance.CheckInTime != checkIn || attendance.CheckOutTime != checkOut || attendance.WorkingMinutes != workingMinutes || attendance.Remarks != correctAttendanceDto.Remark.Trim();
        if (!hasChanges)
        {
            return false;
        }
        attendance.AttendanceDate = correctedDate;
        attendance.CheckInTime = checkIn;
        attendance.CheckOutTime = checkOut;
        attendance.WorkingMinutes = workingMinutes;
        attendance.Remarks = correctAttendanceDto.Remark.Trim();
        attendance.UpdatedAt = DateTime.UtcNow;
        await _mySqlDbContext.SaveChangesAsync(cancellationToken);

        return true;

    }

    private async Task<Dictionary<Guid, Attendance>> AttendanceForDateAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var attendance = await _mySqlDbContext.Attendances.AsNoTracking().Where(row => row.AttendanceDate == date).ToListAsync(cancellationToken);

        return attendance.ToDictionary(row => row.UserId);
    }

    private static AttendanceHistoryDto ToAttendanceHistory(Guid userId, string fullName, DateOnly date, Attendance? attendance)
    {
        var checkedIn = attendance?.CheckInTime != null;
        return new AttendanceHistoryDto
        {
            UserID = userId,
            FullName = fullName,
            AttendanceID = attendance?.AttendanceId ?? Guid.Empty,
            AttendanceDate = date,
            CheckInTime = attendance?.CheckInTime,
            CheckOutTime = checkedIn ? attendance!.CheckOutTime : null,
            WorkingMinutes = checkedIn ? attendance!.WorkingMinutes : 0,
            Status = checkedIn ? attendance!.Status : AbsentStatus,
            Remarks = attendance?.Remarks
        };
    }
}
