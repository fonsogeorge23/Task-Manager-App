using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.Models
{
    /// <summary>
    /// Primary Task entity. Contains ProjectId, VisibilityId, StatusId, PriorityId.
    /// </summary>
    public class TaskObject : Base
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; } = string.Empty;

        [Required]
        public CurrentTaskStatus Status { get; set; } = CurrentTaskStatus.Pending;

        [Required]
        public PriorityLevel Priority { get; set; } = PriorityLevel.Low;

        public DateTime? DueDate { get; set; }

        // Project scoping
        [Required]
        public int ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public Project? Project { get; set; }

        // Assigned User
        public int? AssignedToUserId { get; set; }

        [ForeignKey(nameof(AssignedToUserId))]
        public User? AssignedToUser { get; set; }

        // Task Visibility
        [Required]
        public int TaskVisibilityId { get; set; }
        [Required]
        public TaskVisibility? Visibility { get; set; }

        // Navigation
        public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();

        public ICollection<TaskUser> TaskUsers { get; set; } = new List<TaskUser>();
    }
}
