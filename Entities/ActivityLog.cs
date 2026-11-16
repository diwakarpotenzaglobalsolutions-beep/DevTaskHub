using System.ComponentModel.DataAnnotations;

namespace DevTaskHubBackend.Entities;

public class ActivityLog : BaseEntity
{
    [Key]
    public Guid ActivityLogId { get; set; }

    public Guid ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public Guid? TaskId { get; set; }

    public string? TaskTitle { get; set; }

    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public Guid? TargetUserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
