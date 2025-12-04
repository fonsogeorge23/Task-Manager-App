using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.DTOs.Requests
{
    public class TaskRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty ;
        public CurrentTaskStatus Status { get; set; } = CurrentTaskStatus.Pending;
        public PriorityLevel PriorityLevel { get; set; } = PriorityLevel.Medium;
        public DateTime DueDate { get; set; }
        public int UserId { get; set; } 
    }
}
