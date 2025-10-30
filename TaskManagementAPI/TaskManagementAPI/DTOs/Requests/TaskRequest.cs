using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.DTOs.Requests
{
    public class TaskRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty ;
        public CurrentStatus Status { get; set; } = CurrentStatus.Pending;
        public PriorityLevel PriorityLevel { get; set; } = PriorityLevel.Medium;
        public DateTime DueDate { get; set; }
        public bool IsActive { get; set; } = true;
        public int UserId { get; set; } 
    }
}
