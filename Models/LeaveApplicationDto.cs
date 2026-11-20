using System;
using System.ComponentModel.DataAnnotations;

namespace DevTaskHubBackend.Models;

public class ApplyLeaveDto
{
    [Required(ErrorMessage = "Leave type is required.")]
    public Guid LeaveTypeId { get; set; }

    [Required(ErrorMessage = "Leave start date is required.")]
    public DateOnly StartDate { get; set; }

    [Required(ErrorMessage = "Leave end date is required.")]
    public DateOnly EndDate { get; set; }

    [Required(ErrorMessage = "Leave reason is required.")]
    [StringLength(500, ErrorMessage = "Leave reason cannot exceed 500 characters.")]
    public string Reason { get; set; } = string.Empty;
}

 

public record AvailableLeaveTypeDto(Guid LeaveTypeId, string Name);

public class MyLeaveDto
{
    public Guid LeaveRequestId { get; set; }

    public Guid LeaveTypeId { get; set; }

    public string LeaveTypeName { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal TotalDays { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? RejectionReason { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
