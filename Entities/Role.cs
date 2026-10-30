using System.ComponentModel.DataAnnotations;

namespace DevTaskHubBackend.Data;

public class RoleDB
{
    [Key]
    public Guid RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;
}
