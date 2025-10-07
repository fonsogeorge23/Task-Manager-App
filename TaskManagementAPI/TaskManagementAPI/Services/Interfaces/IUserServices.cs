using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.DTOs.Responses;
using TaskManagementAPI.Models;

namespace TaskManagementAPI.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserResponse?> GetUserByUsernameAsync(string username);
        Task<UserResponse> CreateUserAsync(UserCreateRequest request);
    }
}
