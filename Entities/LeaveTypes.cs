namespace DevTaskHubBackend.Entities;

public class LeaveTypes
{
    public Guid LeaveTypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; }=string.Empty;

    public decimal AnnualQuota { get; set; }

    public bool IsPaid { get; set; }

    public bool AllowHalfDay { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
