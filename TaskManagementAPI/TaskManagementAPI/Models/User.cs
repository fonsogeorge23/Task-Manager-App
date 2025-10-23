using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.Models
{
    public class User
    {
        public int Id { get; set; }     // Primary Key
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        // User role
        public UserRole Role { get; set; } = UserRole.User;

        // Navigation property for tasks created/owned by the user
        public ICollection<TaskObject> Tasks { get; set; } = new List<TaskObject>();
    }
}
