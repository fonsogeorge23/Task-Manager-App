using AutoMapper;
using System.Runtime.InteropServices;
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
        Task<Result<UserResponse>> CreateUserAsync(UserRequest request, int creatorId);

        // Method to authenticate user 
        Task<Result<User>> AuthenticateUserAsync(string username, string password);

        // Method to generate token for authenticated user
        Task<Result<UserResponse>> GenerateToken(User user);

        // Method to get all users
        Task<Result<IEnumerable<UserResponse>>> GetAllUsers();

        // Method to retrieve all users
        Task<Result<IEnumerable<UserResponse>>> GetAllActiveUsers();

        // Method to retrieve user by ID
        Task<Result<UserResponse>> GetActiveUserByIdAsync(int accessId, int id);

        // Method to retrieve user by username
        Task<Result<UserResponse>> GetUserByUsername(UserRequest request);

        // Method to retrieve user by username
        Task<Result<UserResponse>> GetUserByEmail(UserRequest request);

        // Method to activate a user
        Task<Result<UserResponse>> ActivateUser(int accessingId, int userId);
        // Method for Admin to update user
        Task<Result<UserResponse>> AdminUserUpdate(int updateId, UserRequest request);

        // Method for user to update own profile
        Task<Result<UserResponse>> UserProfileUpdate(int userId, UserRequest request);

        // Method to delete user (hard delete)
        Task<Result<string>> HardDeleteUserAsync(int deletingUserId, UserRequest request);

        // Method to inactivate user (soft delete)
        Task<Result<UserResponse>> SoftDeleteUserAsync(int updateUserId, UserRequest request);

    }
    public class UserServices : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IJwtAuthManager _jwtAuthManager;

        public UserServices(IUserRepository userRepository, IMapper mapper, IJwtAuthManager jwtAuthManager)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _jwtAuthManager = jwtAuthManager;
        }

        public async Task<Result<UserResponse>> CreateUserAsync(UserRequest request, int creatorId)
        {
            var validRequest = await ValidateRequestAsync(request);
            if(!validRequest.IsSuccess)
            {
                return Result<UserResponse>.Failure(validRequest.Message);
            }

            // Map UserRequest to User entity
            var user = _mapper.Map<User>(validRequest.Data);

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Save user to the database
            var createdUser = await _userRepository.RegisterUserAsync(user);

            // Map and return success result
            var userResponse = _mapper.Map<UserResponse>(createdUser);

            return Result<UserResponse>.Success(userResponse, "New user registered successfully");
        }

        public async Task<Result<User>> AuthenticateUserAsync(string username, string password)
        { 

            // FIX: Check if the user is null OR if PasswordHash is null/empty BEFORE calling BCrypt.Verify
            if (string.IsNullOrEmpty(username)|| string.IsNullOrEmpty(password))
            {
                return Result<User>.Failure("Invalid username or password.");
            }
            var user = await _userRepository.GetUserCredentialsAsync(username, password);

            if(user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return Result<User>.Failure("Invalid username or password.");
            }

            // Successful authentication
            return Result<User>.Success(user);
        }

        public async Task<Result<UserResponse>> GenerateToken(User user)
        {
            var token = await _jwtAuthManager.GenerateToken(
                user.Id,
                user.Username,
                user.Role.ToString()
            );
            return Result<UserResponse>.Success(new UserResponse
            {
                Id = user.Id,
                UserName = user.Username,
                Email = user.Email,
                Role = user.Role
            }, token);
        }

        public async Task<Result<IEnumerable<UserResponse>>> GetAllUsers()
        {
            var users = await _userRepository.GetAllUsersAsync();

            return ProcessUserListResult(users, "No user found");
        }

        public async Task<Result<IEnumerable<UserResponse>>> GetAllActiveUsers()
        {
            var users = await _userRepository.GetAllActiveUsersAsync();
            return ProcessUserListResult(users, "No active users");
        }

        private Result<IEnumerable<UserResponse>> ProcessUserListResult(IEnumerable<User> users, string notFoundMessage)
        {
            if (users == null || !users.Any())
            {
                return Result<IEnumerable<UserResponse>>.Failure(notFoundMessage);
            }
            var userResponses = _mapper.Map<IEnumerable<UserResponse>>(users);
            return Result<IEnumerable<UserResponse>>.Success(userResponses);
        }

        public async Task<Result<UserResponse>> GetActiveUserByIdAsync(int accessId, int id)
        {
            var user = await _userRepository.GetActiveUserByIdAsync(accessId);

            if (user?.Role == UserRole.Admin || accessId == id)
            {
                var result = await _userRepository.GetActiveUserByIdAsync(id);
                if(result == null)
                {
                    return Result<UserResponse>.Failure("No user found");
                }
                var userResponse = _mapper.Map<UserResponse>(result);
                return Result<UserResponse>.Success(userResponse);
            }
            return Result<UserResponse>.Failure("You are not authorized to access user information");
        }

        public async Task<Result<UserResponse>> GetUserByUsername(UserRequest request)
        {
            var user = await _userRepository.GetUserByUsernameAsync(request.Username);
            if (user == null)
            {
                return Result<UserResponse>.Failure("User not found.");
            }
            var userResponse = _mapper.Map<UserResponse>(user);
            return Result<UserResponse>.Success(userResponse);
        }

        public async Task<Result<UserResponse>> GetUserByEmail(UserRequest request)
        {
            var user = await _userRepository.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return Result<UserResponse>.Failure("User not found.");
            }
            var userResponse = _mapper.Map<UserResponse>(user);
            return Result<UserResponse>.Success(userResponse);
        }
        
        public async Task<Result<UserResponse>> ActivateUser(int updateId, int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return Result<UserResponse>.Failure("User not found");
            }
            user.IsActive = true;

            var result = await _userRepository.UpdateUserAsync(user);
            var response = _mapper.Map<UserResponse>(result);
            return Result<UserResponse>.Success(response);
        }

        public async Task<Result<UserResponse>> AdminUserUpdate(int updateId, UserRequest request)
        {
            var user = await GetUser(request);
            if (user == null)
            {
                return Result<UserResponse>.Failure("User not found with provided username or email.");
            }
            var updatedUser = await UpdateUserAsync(updateId, user, request);
            return updatedUser;
        }

        public async Task<Result<UserResponse>> UserProfileUpdate(int userId, UserRequest request)
        {
            var user = await GetUser(request);
            if(user?.Id != userId)
            {
                return Result<UserResponse>.Failure("You are not authorized to update the profile");
            }
            if (user == null)
            {
                return Result<UserResponse>.Failure("User not found.");
            }
            var updatedUser = await UpdateUserAsync(userId, user, request);
            return updatedUser;
        }

        // Helper method to get user by username or email
        private async Task<User?> GetUser(UserRequest request)
        {
            var user = await _userRepository.GetUserByUsernameAsync(request.Username);
            if (user == null)
            {
                user = await _userRepository.GetUserByEmailAsync(request.Email);
            }
            return user;
        }

        // Helper method to update user details
        private async Task<Result<UserResponse>> UpdateUserAsync(int updateId, User user, UserRequest request)
        {
            // Update fields
            user.Username = request.Username;
            user.Email = request.Email;
            user.Role = ValidateRole(request.Role);

            // Only update password if provided
            if (!string.IsNullOrEmpty(request.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }
            var updatedUser = await _userRepository.UpdateUserAsync(user);
            var userResponse = _mapper.Map<UserResponse>(updatedUser);
            return Result<UserResponse>.Success(userResponse);
        }

        public async Task<Result<string>> HardDeleteUserAsync(int deletingUserId, UserRequest request)
        {
            bool authorized = await AuthorizedUser(deletingUserId, request);
            if (authorized)
            {
                var existingUser = await GetUser(request);
                if (existingUser == null)
                {
                    return Result<string>.Failure("Invalid user information");
                }

                var result = await _userRepository.DeleteUserAsync(existingUser);

                return result ? Result<string>.Success("User deleted successfully.")
                                : Result<string>.Failure("Failed to delete user.");
            }
            return Result<string>.Failure("Invalid/Unauthorized user information");
        }

        public async Task<Result<UserResponse>> SoftDeleteUserAsync(int updateUserId, UserRequest request)
        {
            bool authorized = await AuthorizedUser(updateUserId, request);
            if (authorized)
            {
                var existingUser = await GetUser(request);
                if (existingUser == null)
                {
                    return Result<UserResponse>.Failure("Invalid user information");
                }

                existingUser.IsActive = false;
                var result = await _userRepository.UpdateUserAsync(existingUser);
                var user = _mapper.Map<UserResponse>(result);

                return Result<UserResponse>.Success(user);
            }
            return Result<UserResponse>.Failure("Invalid/Unauthorized user information");
        }

        // Helper method to check if the accessing user is authorized
        private async Task<bool> AuthorizedUser(int id, UserRequest request)
        {
            var accessingUser = await _userRepository.GetActiveUserByIdAsync(id);
            var accessedUserRecord = await GetUser(request);

            if (accessingUser != null && accessedUserRecord != null)
            {
                if (accessingUser.Role == UserRole.Admin || id == accessedUserRecord.Id)
                {
                    return true;
                }
            }
            return false;
        }

        // Helper method to validate and parse role
        private UserRole ValidateRole(string role)
        {
            if (!Enum.TryParse<UserRole>(role, true, out var parsedRole))
            {
                return UserRole.Guest;
            }
            return parsedRole;
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
            var existingUser = await GetUserByUsername(request);
            if(existingUser.IsSuccess)
            {
                return Result<UserRequest>.Failure("Username/Email already exists.");
            }
            else
            {
                existingUser = await GetUserByEmail(request);
                if(existingUser.IsSuccess)
                {
                    return Result<UserRequest>.Failure("Username/Email already exists.");
                }
            }

            var requestRole = ValidateRole(request.Role);
            request.Role = requestRole.ToString();
            return Result<UserRequest>.Success(request);
        }
    }
}