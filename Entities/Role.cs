using System.ComponentModel.DataAnnotations;

namespace DevTaskHubBackend.Data;

public class Role
{
    [Key]
    public Guid RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;
}
