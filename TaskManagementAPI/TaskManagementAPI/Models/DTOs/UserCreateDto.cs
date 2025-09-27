using TaskManagementAPI.Static;

namespace TaskManagementAPI.Models.DTOs
{
    public class UserCreateDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Guest;
    }
}
