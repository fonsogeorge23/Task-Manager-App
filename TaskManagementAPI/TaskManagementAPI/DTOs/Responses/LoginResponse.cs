namespace TaskManagementAPI.DTOs.Responses
{
    public class LoginResponse
    {
        public UserResponse User { get; set; } = null!;
        public string Token { get; set; } = string.Empty;
    }
}
