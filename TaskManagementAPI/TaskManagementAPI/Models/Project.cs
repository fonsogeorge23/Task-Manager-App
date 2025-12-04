using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementAPI.Models
{
    /// <summary>
    /// Projects container. Admin creates projects; PM/User/Guests are members via ProjectMembers.
    /// </summary>
    public class Project: Base
    {
        public int Id { get; set; }

        [Required, MaxLength(250)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Manager of the project
        public int ManagerId { get; set; }

        [ForeignKey(nameof(ManagerId))]
        public User? Manager { get; set; }

        // Navigation property for tasks under this project
        public ICollection<TaskObject> Tasks { get; set; } = new List<TaskObject>();
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
    }
}
