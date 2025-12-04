using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementAPI.Models
{
    public class AuditLog
    {
        public long Id { get; set; } // Primary key

        [Required]
        public int UserId { get; set; } // Who performed the action

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required, MaxLength(50)]
        public string Action { get; set; } = string.Empty; // Create, Update, Delete

        [Required, MaxLength(100)]
        public string EntityName { get; set; } = string.Empty; // TaskObject, Project, etc.

        [Required]
        public int EntityId { get; set; } // ID of affected record

        public string? Details { get; set; } // Optional JSON or description of changes

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // When the action happened
    }
}
