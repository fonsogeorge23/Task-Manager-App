using System.ComponentModel.DataAnnotations;

namespace TaskManagementAPI.Models
{
    public abstract class Base
    {
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int? UpdatedBy { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Row version for optimistic concurrency control.
        /// </summary>
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
