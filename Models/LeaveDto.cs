using System;
using System.ComponentModel.DataAnnotations;

namespace DevTaskHubBackend.Models;

public class CreateLeaveTypeDto
{
    [Required(ErrorMessage = "Leave type name is required.")]
    [StringLength(50, ErrorMessage = "Leave type name cannot exceed 50 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Leave type description is required.")]
    [StringLength(150, ErrorMessage = "Description cannot exceed 150 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Annual leave quota is required.")]
    [Range(0.01, 365, ErrorMessage = "Annual quota must be between 0.01 and 365.")]
    public decimal AnnualQuota { get; set; }

    [Required(ErrorMessage = "Please specify whether this leave type is paid.")]
    public bool IsPaid { get; set; }

    [Required(ErrorMessage = "Please specify whether half-day leave is allowed.")]
    public bool IsHalfDay { get; set; }
}


public class GetLeaveTypeDto
{
    public Guid LeaveTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal AnnualQuota { get; set; }
    public bool IsPaid { get; set; }
    public bool AllowHalfDay { get; set; }
    public bool IsActive { get; set; }
}



