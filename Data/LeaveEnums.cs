namespace DevTaskHubBackend.Data;

public enum LeaveStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum LeaveDurationType
{
    FullDay = 1,
    FirstHalf = 2,
    SecondHalf = 3
}

public enum LeaveTypeEnum
{
    Casual = 1,
    Emergency = 2,
    Vacation = 3,
    Unpaid = 4
}
