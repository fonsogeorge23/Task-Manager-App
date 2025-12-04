using TaskManagementAPI.Models;
using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.DTOs.Responses
{
    public class TaskResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public CurrentTaskStatus Status { get; set; }
        public PriorityLevel Priority { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreateDate { get; set; }
        public bool IsActive { get; set; }
        public int UserId { get; set; }
        public string Owner { get; set; } = string.Empty;
    }
}
