using System.ComponentModel.DataAnnotations;

namespace DevTaskHubBackend.Entities;

public class ProjectMemberDB : BaseEntity
{
    [Key]
    public Guid ProjectMemberId { get; set; }

    public Guid ProjectId { get; set; }

    public Guid UserId { get; set; }
}
