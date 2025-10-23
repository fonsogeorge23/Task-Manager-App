using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.Models
{
    public class TaskObject
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public CurrentStatus Status { get; set; } = CurrentStatus.Pending;
        public PriorityLevel Priority { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.Now;

        public int UserId {  get; set; }
        public User User { get; set; } = null!;
    }
}
