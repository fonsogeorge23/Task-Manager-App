namespace TaskManagementAPI.Static
{
    public enum CurrentStatus
    {
        Pending,
        InProgress,
        Completed,
        Archived
    }

    public enum PriorityLevel
    {
        High, 
        Medium, 
        Low
    }

    public enum UserRole
    {
        Admin,
        PM,
        User, 
        Guest
    }
}
