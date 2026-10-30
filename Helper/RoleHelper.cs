using DevTaskHubBackend.Data;

namespace DevTaskHubBackend.Helpers;

public static class RoleHelper
{
    public static string GetRoleName(Guid roleId)
    {
        return roleId switch
        {
            var id when id == UserRoles.AdminId =>
                UserRole.Admin.ToName(),

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