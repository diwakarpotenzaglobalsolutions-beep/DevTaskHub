using DevTaskHubBackend.Data;
using Microsoft.AspNetCore.Authorization;

namespace DevTaskHubBackend.Helpers;

public class RoleAuthorizeAttribute : AuthorizeAttribute
{
    public RoleAuthorizeAttribute(params UserRole[] roles)
    {
        // Admin always has access
        var allowedRoles = new List<UserRole>
        {
            UserRole.Admin
        };

        allowedRoles.AddRange(roles);

        Roles = string.Join(",",
            allowedRoles
                .Distinct()
                .Select(role => role.ToName()));
    }
}