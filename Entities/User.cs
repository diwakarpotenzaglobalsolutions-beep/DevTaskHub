using System.ComponentModel.DataAnnotations;

namespace DevTaskHubBackend.Entities;

public class User : BaseEntity
{
    [Key]
    public Guid UserID { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; }= string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public Guid RoleId { get; set; }

    public Guid? ManagerId { get; set; }

    public string? GitHubUserName { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public uint RowVersion { get; set; }

      public string RefreshToken { get; set; }=string.Empty;

  public DateTime RefreshTokenExpiry {get;set;} = DateTime.UtcNow;
}
