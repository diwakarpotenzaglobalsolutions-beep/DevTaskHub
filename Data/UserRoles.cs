namespace DevTaskHubBackend.Data;

public enum UserRole
{
    Admin,
    Manager,
    Developer
}

public static class UserRoles
{
    public static readonly Guid AdminId = new("8f4c2a10-6b3e-4d91-9c27-1a5e7b0d4f21");
    public static readonly Guid ManagerId = new("3c9e1b74-2a68-4f05-b8d3-6e1c9a4d7b52");
    public static readonly Guid DeveloperId = new("d17a6e83-5c42-4b19-a0f6-9b3d8e2c1a47");

    public static readonly string[] All =
    [
        UserRole.Admin.ToName(),
        UserRole.Manager.ToName(),
        UserRole.Developer.ToName()
    ];

    public static string ToName(this UserRole role) => role switch
    {
        UserRole.Admin => "Admin",
        UserRole.Manager => "Project Manager",
        UserRole.Developer => "Developer",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown user role.")
    };

    public static Guid ToId(this UserRole role) => role switch
    {
        UserRole.Admin => AdminId,
        UserRole.Manager => ManagerId,
        UserRole.Developer => DeveloperId,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown user role.")
    };
}

public enum TaskStatus
{
    ToDo,
    InProgress,
    InReview,
    Done,
    Blocked
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum ProjectStatus
{
    Active,
    OnHold,
    Completed,
    Archived
}

public enum PullRequestStatus
{
    Open,
    Merged,
    Closed
}
