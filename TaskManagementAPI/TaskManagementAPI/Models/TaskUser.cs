namespace TaskManagementAPI.Models
{
    public class TaskUser:Base
    {
        public int Id { get; set; }

        public int TaskId { get; set; }
        public TaskObject Task { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public int AssignedBy { get; set; }
    }

}
