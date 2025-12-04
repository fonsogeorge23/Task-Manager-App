using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementAPI.Models
{
    /// <summary>
    /// Comments / suggestions on a task. Guests can add comments if they have permission for the task's visibility.
    /// </summary>
    public class TaskComment:Base
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int TaskId { get; set; }

        [ForeignKey(nameof(TaskId))]
        public TaskObject? Task { get; set; }

        [Required]
        public int AuthorId { get; set; }
        [ForeignKey(nameof(AuthorId))]
        public User? Author { get; set; }

    }
}
