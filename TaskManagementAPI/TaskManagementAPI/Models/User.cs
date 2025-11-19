using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.Models
{
    public class User : Base
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }     // Primary Key

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Guest;


        // Navigation property for tasks created/owned by the user
        public ICollection<TaskObject> Tasks { get; set; } = new List<TaskObject>();
    }
}
