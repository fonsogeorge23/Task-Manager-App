using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.DTOs.Requests
{
    public class UserCreateRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Guest;
    }
}
