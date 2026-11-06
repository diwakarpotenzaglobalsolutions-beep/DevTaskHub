using System;

namespace DevTaskHubBackend.Models;

public class RoleAssignDto
{
   public Guid UserID { get; set; }
   public Guid RoleID{ get; set; }
}

public class RoleListDto
{
    public Guid RoleID { get; set; }
    public string RoleName { get; set; }=string.Empty;
}
