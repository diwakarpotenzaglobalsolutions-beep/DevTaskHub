using System.ComponentModel.DataAnnotations;

namespace DevTaskHubBackend.Entities;

public class Attendance
{
    [Key]
    public Guid AttendanceId { get; set; }

    public Guid UserId { get; set; }

    public DateOnly AttendanceDate { get; set; }

    public DateTime? CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public int WorkingMinutes { get; set; }

    public string Status { get; set; } = "Present";

    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
