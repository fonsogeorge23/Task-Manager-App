using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.DTOs.Responses;
using TaskManagementAPI.Models;
using TaskManagementAPI.Repositories.Interfaces;
using TaskManagementAPI.Services.Interfaces;

namespace TaskManagementAPI.Services.Implementations
{
    public class UserServices : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserServices(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserResponse?> GetUserByUsernameAsync(string username)
        {
            var user = await _userRepository.GetUserByUsernameAsync(username);
            if (user == null) return null;

            // Map User -> UserResponse
            return new UserResponse
            {
                Id = user.Id,
                UserName = user.Username,
                Email = user.Email,
                Role = user.Role
            };
        }

        public async Task<UserResponse> CreateUserAsync(UserCreateRequest request)
        {
            var user = new  User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role
            };

            await _userRepository.AddUserAsync(user);

            return new UserResponse
            {
                Id = user.Id,
                UserName = user.Username,
                Email = user.Email,
                Role = user.Role
            };
        }
    }
}
