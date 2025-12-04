using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementAPI.Models
{
    /// <summary>
    /// Task visibility levels are dynamic (DB-driven) to allow adding new levels later.
    /// Examples: InternalOnly, ClientVisible, ContractorVisible, PublicDemo
    /// </summary>
    public class VisibilityLevel
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Navigation
        public ICollection<TaskObject> Tasks { get; set; } = new List<TaskObject>();
    }
}
