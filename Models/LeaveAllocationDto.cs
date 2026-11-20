using System;
using System.ComponentModel.DataAnnotations;

namespace DevTaskHubBackend.Models;

public class AllocateLeaveBalanceDto
{
    [Required(ErrorMessage = "User ID is required.")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Leave type ID is required.")]
    public Guid LeaveTypeId { get; set; }

    [Required(ErrorMessage = "Year is required.")]
    [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100.")]
    public int Year { get; set; }

    [Required(ErrorMessage = "Allocated days are required.")]
    [Range(typeof(decimal), "0", "365",ErrorMessage = "Allocated days must be between 0 and 365.")]
    public decimal AllocatedDays { get; set; }
}


public class EmployeeLeaveBalanceDto
{
    public Guid UserId { get; set; }
    public Guid LeaveTypeId { get; set; }

    public string LeaveTypeName { get; set; } = string.Empty;

    public int Year { get; set; }

    public decimal AllocatedDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal PendingDays { get; set; }
    public decimal RemainingDays { get; set; }
}
