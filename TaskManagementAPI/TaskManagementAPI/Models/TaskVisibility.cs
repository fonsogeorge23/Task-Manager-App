using System.ComponentModel.DataAnnotations;

namespace TaskManagementAPI.Models
{
    public class TaskVisibility
    {
        /// <summary>
        /// Primary key for the visibility entry.
        /// </summary>
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        // Navigation property for tasks with this visibility
        public ICollection<TaskObject> Tasks { get; set; } = new List<TaskObject>();
    }
}
