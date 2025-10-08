namespace TaskManagementAPI.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(int userId, string username, string role);
    }
}
