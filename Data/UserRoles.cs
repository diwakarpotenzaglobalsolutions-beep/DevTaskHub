namespace DevTaskHubBackend.Data;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Project Manager";
    public const string Developer = "Developer";

    public static readonly Guid AdminId = new("8f4c2a10-6b3e-4d91-9c27-1a5e7b0d4f21");
    public static readonly Guid ManagerId = new("3c9e1b74-2a68-4f05-b8d3-6e1c9a4d7b52");
    public static readonly Guid DeveloperId = new("d17a6e83-5c42-4b19-a0f6-9b3d8e2c1a47");
}

public static class TaskStatuses
{
    public const string ToDo = "ToDo";
    public const string InProgress = "InProgress";
    public const string InReview = "InReview";
    public const string Done = "Done";
    public const string Blocked = "Blocked";

    public static readonly string[] All = [ToDo, InProgress, InReview, Done, Blocked];
}

public static class TaskPriorities
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Critical = "Critical";

    public static readonly string[] All = [Low, Medium, High, Critical];
}

public static class ProjectStatuses
{
    public const string Active = "Active";
    public const string OnHold = "OnHold";
    public const string Completed = "Completed";
    public const string Archived = "Archived";

    public static readonly string[] All = [Active, OnHold, Completed, Archived];
}

public static class PullRequestStatuses
{
    public const string Open = "Open";
    public const string Merged = "Merged";
    public const string Closed = "Closed";

    public static readonly string[] All = [Open, Merged, Closed];
}
