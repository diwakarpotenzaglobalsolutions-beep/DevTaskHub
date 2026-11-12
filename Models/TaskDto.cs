using System;
using System.ComponentModel.DataAnnotations;
using DevTaskHubBackend.Data;
using TaskStatusEnum = DevTaskHubBackend.Data.TaskStatus;
namespace DevTaskHubBackend.Models;

public class CreateTaskDto
{
    [Required]
    public Guid ProjectID { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public TaskPriority Priority { get; set; }

    public DateTime DueDate { get; set; }
}

public class GetTaskResponseDto
{
    public Guid TaskID { get; set; }

    public Guid ProjectID { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? TaskSummary { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;
    public Guid? AssignedToUserId { get; set; }
    public string? AssignedToName { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? GitHubPRNumber { get; set; }
    public string? GitHubPRStatus { get; set; }
    public string? GitHubPRUrl { get; set; }
}

public class EditTaskDto
{
    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    [EnumDataType(typeof(TaskPriority))]
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
}

public class AssignTaskDto
{
    [Required]
    public Guid TaskID { get; set; }

    [Required]
    public Guid DeveloperID { get; set; }
}

 public class UpdateTaskStatusDto
{
    [Required]
    [EnumDataType(typeof(TaskStatusEnum))]
    public TaskStatusEnum Status { get; set; }
}