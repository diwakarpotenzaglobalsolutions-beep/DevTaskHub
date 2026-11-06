using System;

namespace DevTaskHubBackend.Models;

public class DashboardResponseDto
{
    public int TotalProjects { get; set; }
    public int TotalTasks { get; set; }
    public int ToDoTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int InReviewTasks { get; set; }
    public int DoneTasks { get; set; }
    public int BlockedTasks { get; set; }

    public int? TotalUsers { get; set; }
    public int? ActiveUsers { get; set; }
    public int? InactiveUsers { get; set; }

    public int? TotalManagers { get; set; }
    public int? ActiveManagers { get; set; }
    public int? InactiveManagers { get; set; }

    public int? TotalDevelopers { get; set; }
    public int? ActiveDevelopers { get; set; }
    public int? InactiveDevelopers { get; set; }
}
