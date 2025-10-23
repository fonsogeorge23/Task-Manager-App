using AutoMapper;
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
        Task<Result<UserResponse>> CreateUserAsync(UserRequest request);

        // Method to authenticate user 
        Task<Result<UserResponse>> AuthenticateUserAsync(string username, string password);
    }
    public class UserServices : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UserServices(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        // Updated signature to use the Result<T> pattern
        public async Task<Result<UserResponse>> CreateUserAsync(UserRequest request)
        {
            // Check if username already exists
            var existingUser = await _userRepository.GetUserByUsernameAsync(request.Username);

            if (existingUser != null)
            {
                // Return failure result instead of an HTTP BadRequest
                return Result<UserResponse>.Failure("Username already exists.");
            }

            // Map UserRequest to User entity
            var user = _mapper.Map<User>(request);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Save user to the database
            var createdUser = await _userRepository.AddUserAsync(user);

            // Map and return success result
            var userResponse = _mapper.Map<UserResponse>(createdUser);

            return Result<UserResponse>.Success(userResponse);
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

            // Successful authentication
            var response = _mapper.Map<UserResponse>(user);

            return Result<UserResponse>.Success(response);
        }
    }
}