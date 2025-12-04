using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.Models
{
    public class ProjectMember:Base
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public Project Project { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public ProjectRole ProjectRole { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public int AssignedBy { get; set; }
    }
}
