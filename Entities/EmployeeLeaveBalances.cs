namespace DevTaskHubBackend.Entities;

public class EmployeeLeaveBalances
{
    public Guid EmployeeLeaveBalanceId { get; set; }

    public Guid UserId { get; set; }

    public Guid LeaveTypeId { get; set; }

    public int Year { get; set; }

    public decimal AllocatedDays { get; set; }

    public decimal UsedDays { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;

    public LeaveTypes LeaveType { get; set; } = null!;
}
