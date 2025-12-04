using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.Models
{
    /// <summary>
    /// Application user (internal or external). Roles are assigned via UserRoles table.
    /// Add additional fields for external users (CompanyName, InvitationToken, ExpiresAt, etc.) as needed.
    /// </summary>
    public class User : Base
    {
        public int Id { get; set; }     // Primary Key

        [Required, MaxLength(150)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(320)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Password hash (or external identity reference).
        /// For external federated users you may leave this null and use an IdentityProvider field instead.
        /// </summary>
        public string? PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Guest;

        // Navigation property for projects owned/managed by the user
        public ICollection<Project> ProjectsManaging { get; set; } = new List<Project>();

        // Navigation property for comments made by the user
        public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();

        // Navigation property for Project Members for project assigned
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

        // Navigation property for tasks created/owned by the user
        public ICollection<TaskUser> TaskUsers { get; set; } = new List<TaskUser>();
    }
}
