using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Entities;

namespace DevTaskHubBackend.Models;

public class CreateProjectDto
{

    [Required(ErrorMessage ="Project name is required.")]
    [StringLength(150)]
    public string ProjectName { get; set; }=string.Empty;

    [StringLength(1000)]
    public string Description { get; set; }=string.Empty;
}

public class CreateProjectResponseDto:BaseEntity
{
    public Guid ProjectId { get; set; }

    public string ProjectName { get; set; }=string.Empty;

    public string Description { get; set; }=string.Empty;

    public string Status { get; set; }=string.Empty;

    public Guid CreteByUserId { get; set; }
}

 

public class GetProjectResponseDto
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public Guid AssignedToUserId { get; set; }
    public string AssignedToName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ProjectListDto
{
    public List<GetProjectResponseDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class EditProjectDto
{
    [Required(ErrorMessage ="Project name is required.")]
    [StringLength(150)]

    public  string ProjectName { get; set; }=string.Empty;

    [Required(ErrorMessage ="Description is required.")]
    [StringLength(1000)]

    public string Description { get; set; }=string.Empty;

    [Required(ErrorMessage ="ProjectStatus is required.")]
    [EnumDataType(typeof(ProjectStatus))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProjectStatus Status { get; set; }
}


public class ReAsignProjectDto
{
    [Required]
    public Guid ProjectID { get; set; }
    [Required]
    public Guid AssignedToUserID { get; set; }
}

public class AssignDevelopersDto
{
    [Required]
    public Guid ProjectID { get; set; }
    [Required]
    [MinLength(1, ErrorMessage = "Please select at least one developer.")]
    public List<Guid> DeveloperIDs { get; set; } = new();
}

public class ProjectDevloperDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? GitHubAvatarUrl { get; set; }
}




