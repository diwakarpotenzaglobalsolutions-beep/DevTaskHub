using System.Security.Claims;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Exceptions;

namespace DevTaskHubBackend.Helpers;

public static class RoleHelper
{
    public static string GetRoleName(Guid roleId)
    {
        string v = UserRole.Admin.ToName();
        return roleId switch
        {
            var id when id == UserRoles.AdminId =>
                v,

            var id when id == UserRoles.ManagerId =>
                UserRole.Manager.ToName(),

            var id when id == UserRoles.DeveloperId =>
                UserRole.Developer.ToName(),

            _ => throw new ArgumentException(
                "Invalid RoleId.",
                nameof(roleId))
        };
    }
}

 

public static class UserClaimsHelper
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(userIdValue, out var userId))
        {
            return userId;
        }

        throw new AppException(StatusCodes.Status401Unauthorized, "User is not unauthorized.");
    }
}