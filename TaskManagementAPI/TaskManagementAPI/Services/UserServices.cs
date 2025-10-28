using AutoMapper;
using Microsoft.AspNetCore.Authorization;
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

        // Method to retrieve all users
        Task<IEnumerable<UserResponse>> GetAllUsersAsync();

        // Method to retrieve user by ID
        Task<Result<UserResponse>> GetUserByIdAsync(int id);

        // Method to update user
        Task<Result<UserResponse>> UpdateUserAsync(int id, UserRequest request);
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

        public async Task<Result<UserResponse>> CreateUserAsync(UserRequest request)
        {
            var validRequest = await ValidateRequestAsync(request);
            if(!validRequest.IsSuccess)
            {
                return Result<UserResponse>.Failure(validRequest.ErrorMessage);
            }

            // Map UserRequest to User entity
            var user = _mapper.Map<User>(validRequest.Data);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Save user to the database
            var createdUser = await _userRepository.RegisterUserAsync(user);

            // Map and return success result
            var userResponse = _mapper.Map<UserResponse>(createdUser);

            return Result<UserResponse>.Success(userResponse);
        }

        public async Task<Result<UserResponse>> AuthenticateUserAsync(string username, string password)
        { 

            // FIX: Check if the user is null OR if PasswordHash is null/empty BEFORE calling BCrypt.Verify
            if (string.IsNullOrEmpty(username)|| string.IsNullOrEmpty(password))
            {
                return Result<UserResponse>.Failure("Invalid username or password.");
            }
            var user = await _userRepository.GetUserCredentialsAsync(username, password);

            if(user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return Result<UserResponse>.Failure("Invalid username or password.");
            }

            // Successful authentication
            var response = _mapper.Map<UserResponse>(user);

            return Result<UserResponse>.Success(response);
        }

        public async Task<IEnumerable<UserResponse>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllUsersAsync();
            return _mapper.Map<IEnumerable<UserResponse>>(users);
        }

        public async Task<Result<UserResponse>> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null)
            {
                return Result<UserResponse>.Failure("User not found.");
            }
            var userResponse = _mapper.Map<UserResponse>(user);
            return Result<UserResponse>.Success(userResponse);
        }

        public async Task<Result<UserResponse>> UpdateUserAsync(int id, UserRequest request)
        {
            var existingUser = await _userRepository.GetUserByIdAsync(id);
            if (existingUser == null)
            {
                return Result<UserResponse>.Failure("User not found.");
            }
            // Update fields
            existingUser.Username = request.Username;
            existingUser.Email = request.Email;
            // Only update password if provided
            if (!string.IsNullOrEmpty(request.Password))
            {
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }
            var updatedUser = await _userRepository.UpdateUserAsync(id, existingUser);
            var userResponse = _mapper.Map<UserResponse>(updatedUser);
            return Result<UserResponse>.Success(userResponse);
        }

        // Helper method to validate request and assign role
        private async Task<Result<UserRequest>> ValidateRequestAsync(UserRequest request)
        {
            // Basic validation
            if (request == null)
            {
                return Result<UserRequest>.Failure("Request cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Result<UserRequest>.Failure("Username and password are required.");
            }

            // Checking user existing or not
            var existingUser = await _userRepository.GetUserByUsernameAsync(request.Username);
            if(existingUser != null)
            {
                return Result<UserRequest>.Failure("Username/Email already exists.");
            }
            else
            {
                existingUser = await _userRepository.GetUserByEmailAsync(request.Email);
                if(existingUser != null)
                {
                    return Result<UserRequest>.Failure("Username/Email already exists.");
                }
            }

            var requestRole = ValidateRole(request.Role);
            request.Role = requestRole.ToString();
            return Result<UserRequest>.Success(request);
        }

        private UserRole ValidateRole(string role)
        {
            if (!Enum.TryParse<UserRole>(role, true, out var parsedRole))
            {
                return UserRole.Guest;
            }
            return parsedRole;
        }
    }
}