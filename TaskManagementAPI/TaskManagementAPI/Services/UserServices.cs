using System;
using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.DTOs.Responses;
using TaskManagementAPI.Models;
using TaskManagementAPI.Repositories;
using TaskManagementAPI.Utilities; // Added using statement for Result<T>

namespace TaskManagementAPI.Services
{
    public interface IUserService
    {
        // Updated signature to use the Result<T> pattern
        Task<Result<UserResponse>> CreateUserAsync(UserCreateRequest request);
        Task<User?> GetUserByUsername(string username);
        Task<Result<UserResponse>> AuthenticateUserAsync(string username, string password);
    }
    public class UserServices : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserServices(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // Updated signature to use the Result<T> pattern
        public async Task<Result<UserResponse>> CreateUserAsync(UserCreateRequest request)
        {
            // Check if username already exists
            var existingUser = await _userRepository.GetUserByUsernameAsync(request.Username);

            if (existingUser != null)
            {
                // Return failure result instead of an HTTP BadRequest
                return Result<UserResponse>.Failure("Username already exists.");
            }

            // Create user model
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                // Ensure a hash is always generated on registration
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role
            };

            await _userRepository.AddUserAsync(user);

            // Map and return success result
            var userResponse = new UserResponse
            {
                Id = user.Id,
                UserName = user.Username,
                Email = user.Email,
                Role = user.Role
            };

            return Result<UserResponse>.Success(userResponse);
        }

        public async Task<User?> GetUserByUsername(string username)
        {
            var user = await _userRepository.GetUserByUsernameAsync(username);
            if (user == null) return null;

            // Map User -> UserResponse
            return new User
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            };
        }

        public async Task<Result<UserResponse>> AuthenticateUserAsync(string username, string password)
        {
            var user = await _userRepository.GetUserByUsernameAsync(username);

            // FIX: Check if the user is null OR if PasswordHash is null/empty BEFORE calling BCrypt.Verify
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                // Failed authentication because user doesn't exist or has an invalid password hash
                return null;
            }

            // Only call BCrypt.Verify if we have a hash to compare against
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return null; // Password mismatch
            }

            var response = new UserResponse
            {
                Id = user.Id,
                UserName = user.Username,
                Email = user.Email,
                Role = user.Role
            };
            return Result<UserResponse>.Success(response);
        }
    }
}