using System.ComponentModel.DataAnnotations;
using DevTaskHubBackend.Data;

namespace DevTaskHubBackend.Entities;

public class Projects : BaseEntity
{
    [Key]
    public Guid ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = nameof(ProjectStatus.Active);

    public Guid CreatedByUserId { get; set; }

    public string? GitHubOwner { get; set; }

    public string? GitHubRepoName { get; set; }

    public bool IsDeleted { get; set; }

    public uint RowVersion { get; set; }
}
