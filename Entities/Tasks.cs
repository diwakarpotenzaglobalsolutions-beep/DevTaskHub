using System.ComponentModel.DataAnnotations;
using DevTaskHubBackend.Data;

namespace DevTaskHubBackend.Entities;

public class Tasks : BaseEntity
{
    [Key]
    public Guid TaskId { get; set; }

    public Guid ProjectId { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? TaskSummery { get; set; }

    public string Status { get; set; } = TaskStatuses.ToDo;

    public string Priority { get; set; } = TaskPriorities.Medium;

    public DateTime? DueDate { get; set; }

    public int? GitHubPRNumber { get; set; }

    public string? GitHubPRStatus { get; set; }

    public string? GitHubPRUrl { get; set; }

    public DateTime? LastSyncedAt { get; set; }

    public bool IsDeleted { get; set; }

    public uint RowVersion { get; set; }
}
