using System.ComponentModel.DataAnnotations;

namespace DevTaskHubBackend.Entities;

public class TaskComment : BaseEntity
{
    [Key]
    public Guid TaskCommentId { get; set; }

    public Guid TaskId { get; set; }

    public Guid UserId { get; set; }

    public string Comment { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}
