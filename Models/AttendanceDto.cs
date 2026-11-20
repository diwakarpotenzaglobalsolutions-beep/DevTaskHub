using System;
using System.ComponentModel.DataAnnotations;
using DevTaskHubBackend.Entities;

namespace DevTaskHubBackend.Models;

public class AttendanceDto : BaseEntity
{
    public Guid AttendanceID { get; set; }

    public Guid UserID { get; set; }

    public DateOnly AttendanceDate { get; set; }

    public DateTime? CheckInTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? CheckOutTime { get; set; }

    public int WorkingMinutes { get; set; }
}


public class TodayAttendanceDto
{
    public Guid AttendanceID { get; set; }
    public DateOnly AttendanceDate { get; set; }

    public DateTime? CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public bool IsCheckedIn { get; set; }

    public bool IsCheckedOut { get; set; }

    public int WorkingMinutes { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class AttendanceHistoryDto
{
    public Guid UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public Guid AttendanceID { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public int WorkingMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public class CorrectAttendanceDto
{
    [Required]
    public DateTime   CheckInTime { get; set; }

    [Required]
    public DateTime   CheckOutTime { get; set; }
    [Required]
    [StringLength(500,MinimumLength=5)]
    public string   Remark { get; set; }=string.Empty;
}
