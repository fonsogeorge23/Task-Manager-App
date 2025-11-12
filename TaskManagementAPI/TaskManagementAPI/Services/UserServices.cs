using AutoMapper;
using Azure.Core;
using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.DTOs.Responses;
using TaskManagementAPI.Models;
using TaskManagementAPI.Repositories;
using TaskManagementAPI.Utilities; // Added using statement for Result<T>

namespace TaskManagementAPI.Services
{
    public interface IUserService
    {
        // Method to register new user
        Task<Result<UserResponse>> CreateUserAsync(UserRequest request, int creatorId);

        // Method to authenticate the login credentials
        Task<Result<UserResponse>> AuthenticateUser(LoginRequest request);

        // Method to get all users
        Task<Result<IEnumerable<UserResponse>>> GetAllUsers(int accessId, bool? active);

        // Method to checked authorized user action 
        Task<Result> AuthorizedAction(int userId, int? accessId = null);

        // Method to get any user by userId
        Task<Result<User>> GetUserById(int userId, int? accessId = null);

        // Method to get active user information by userId
        Task<Result<UserResponse>> GetActiveUserById(int userId, int? accessId = null);

        // Method to update the user information 
        Task<Result<UserResponse>> UpdateUser(UserRequest request, int? updatingId = null);

        // Method to activate a user
        Task<Result<UserResponse>> ActivateUser(int userId, int updateId);


















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
            var validRequest = await ValidateUserRequestAsync(request);
            if (!validRequest.IsSuccess)
            {
                return Result<UserResponse>.Failure(validRequest.Message);
            }

            // Map UserRequest to User entity
            var user = _mapper.Map<User>(validRequest.Data);
            user.PasswordHash = await GetHashCode(request.Password);

            // Save user to the database
            var createdUser = await RegisterUser(user);

            return createdUser;
        }

        // Helper method to validate request and assign role
        private async Task<Result<UserRequest>> ValidateUserRequestAsync(UserRequest request)
        {
            // Basic validation
            if (request == null)
            {
                return Result<UserRequest>.Failure("Request cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Result<UserRequest>.Failure("Enter a valid email");
            }

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Result<UserRequest>.Failure("Username and password are required.");
            }

            // Checking user existing or not
            var existingUser = await GetUser(request);
            if (existingUser.IsSuccess)
            {
                return Result<UserRequest>.Failure($"{existingUser.Message} - Invalid request");
            }

            var requestRole = ValidateRole(request.Role);
            request.Role = requestRole.ToString();
            return Result<UserRequest>.Success(request);
        }

        // Helper method to get user by username or email
        private async Task<Result<User>> GetUser(UserRequest request)
        {
            var user = await GetUserByUsername(request.Username);
            if (!user.IsSuccess)
            {
                user = await GetUserByEmail(request.Email);
                if (!user!.IsSuccess)
                {
                    return Result<User>.Failure("No user exist");
                }
            }
            return user;
        }

        // Helper method to get the user by username
        private async Task<Result<User>> GetUserByUsername(string username)
        {
            var user = await _userRepository.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return Result<User>.Failure("User not found.");
            }
            //return await _userRepository.GetUserByUsernameAsync(username);
            return Result<User>.Success(user);
        }

        // Helper method to get the user by email
        private async Task<Result<User>> GetUserByEmail(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                return Result<User>.Failure("User not found.");
            }
            //return await _userRepository.GetUserByEmailAsync(email);
            return Result<User>.Success(user);
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

        private Task<string> GetHashCode(string str)
        {
            // BCrypt.Net.BCrypt.HashPassword is synchronous, so do not use await
            return Task.FromResult(BCrypt.Net.BCrypt.HashPassword(str));
        }

        private async Task<Result<UserResponse>> RegisterUser(User user)
        {
            var newUser = await _userRepository.RegisterUserAsync(user);
            if (newUser == null)
            {
                Result<UserResponse>.Failure("User not registered");
            }
            var response = _mapper.Map<UserResponse>(newUser);
            return Result<UserResponse>.Success(response, "User Registered successfully");
        }


        public async Task<Result<UserResponse>> AuthenticateUser(LoginRequest request)
        {
            var requestUser = await ValidateLoginRequest(request);
            if (!requestUser.IsSuccess)
            {
                return Result<UserResponse>.Failure($"{requestUser.Message} - Failed login");
            }

            var (validUser, token) = await GenerateToken(requestUser.Data!);
            return Result<UserResponse>.Success(validUser.Data!, token);
        }

        // Helper method to validate and authenticate the login request
        private async Task<Result<User>> ValidateLoginRequest(LoginRequest request)
        {
            // Basic Request validation
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return Result<User>.Failure("Invalid Username or password");
            }

            var user = await _userRepository.GetUserCredentialsAsync(request);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Result<User>.Failure("No user/Inactive - check/activate - credentials");
            }

            // Successful authentication
            return Result<User>.Success(user);
        }

        // Helper method to generate token for a valid user login
        private async Task<(Result<UserResponse> Result, string Token)> GenerateToken(User user)
        {
            var generatedToken = await _jwtAuthManager.GenerateToken(
                user.Id,
                user.Username,
                user.Role.ToString()
            );

            var response = _mapper.Map<UserResponse>(user);
            return (Result<UserResponse>.Success(response), generatedToken);
        }
        

        public async Task<Result<IEnumerable<UserResponse>>> GetAllUsers(int accessId, bool? active)
        {
            var users = await GetAllUsersService(accessId, active);
            if (!users.IsSuccess)
            {
                return Result<IEnumerable<UserResponse>>.Failure($"{users.Message} - No users information available");
            }

            return ProcessUserListResult(users.Data!, "No user found");
        }

        // Helper method to get all active/inactive users
        private async Task<Result<IEnumerable<User>>> GetAllUsersService(int accessId, bool? active = null)
        {
            var authorizedAccess = await AuthorizedUser(accessId);
            if (!authorizedAccess.IsSuccess)
            {
                return Result<IEnumerable<User>>.Failure($"{authorizedAccess.Message!} - User info not available");
            }
            IEnumerable<User> usersList;
            if (active == null)
            {
                usersList = await _userRepository.GetAllUsersAsync();
            }
            else
            {
                usersList = await _userRepository.GetAllActiveUsersAsync(active.Value);
            }

            return Result<IEnumerable<User>>.Success(usersList);
        }

        // Helper method to authorize admin to access data
        private async Task<Result> AuthorizedUser(int accessId)
        {
            var user = await _userRepository.GetActiveUserByIdAsync(accessId);
            if (user?.Role == UserRole.Admin)
            {
                return Result.Success();
            }
            return Result.Failure("Failed to authorize the action");
        }
        
        // Helper method to Process user list to User Response
        private Result<IEnumerable<UserResponse>> ProcessUserListResult(IEnumerable<User> users, string notFoundMessage)
        {
            if (users == null || !users.Any())
            {
                return Result<IEnumerable<UserResponse>>.Failure(notFoundMessage);
            }
            var userResponses = _mapper.Map<IEnumerable<UserResponse>>(users);
            return Result<IEnumerable<UserResponse>>.Success(userResponses);
        }


        public async Task<Result<User>> GetUserById(int userId, int? accessId = null)
        {
            // Access control
            var authorizedUser = await AuthorizedAction(userId, accessId);
            if (!authorizedUser.IsSuccess)
            {
                return Result<User>.Failure($"{authorizedUser.Message} - cannot view the user");
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return Result<User>.Failure("No user found");
            }
            return Result<User>.Success(user);
        }


        public async Task<Result<UserResponse>> GetActiveUserById(int userId, int? accessId = null)
        {
            // Access control
            if(accessId != null)
            {
                var authorizedUser = await AuthorizedAction(accessId.Value);
                if (!authorizedUser.IsSuccess)
                {
                    return Result<UserResponse>.Failure($"{authorizedUser.Message} - cannot view the user");
                }
            }


            var user = await _userRepository.GetActiveUserByIdAsync(userId);
            if (user == null)
            {
                return Result<UserResponse>.Failure("User not found / Inactive user.");
            }
            var userResponse = _mapper.Map<UserResponse>(user);
            return Result<UserResponse>.Success(userResponse);
        }
        

        public async Task<Result> AuthorizedAction(int userId, int? accessId = null)
        {
            // base condition
            if (accessId != null && userId == accessId)
            {
                return Result.Success();
            }

            // if admin accessing user info
            return await AuthorizedUser(userId);
        }


        public async Task<Result<UserResponse>> UpdateUser(UserRequest request, int? updatingId = null)
        {
            // Access control
            if(updatingId != null)
            {
                var authorizedToUpdate = await AuthorizedUser(updatingId.Value);
                if (!authorizedToUpdate.IsSuccess)
                {
                    var user = await GetUser(request);
                    if (!user.IsSuccess)
                    {
                        return Result<UserResponse>.Failure($"{authorizedToUpdate.Message} - Failed to update");
                    }
                    authorizedToUpdate = await AuthorizedAction(user.Data!.Id, updatingId);
                    if (!authorizedToUpdate.IsSuccess)
                    {
                        return Result<UserResponse>.Failure($"{authorizedToUpdate.Message} - Failed to update");
                    }
                }
            }

            var updatedUser = await UpdateUserAsync(request);
            if (!updatedUser.IsSuccess)
            {
                return Result<UserResponse>.Failure($"{updatedUser.Message} - Failed to update");
            }
            return updatedUser;
        }

        // Helper method to update user details
        private async Task<Result<UserResponse>> UpdateUserAsync(UserRequest request)
        {
            var user = await GetUser(request);
            if (!user.IsSuccess)
            {
                Result<UserResponse>.Failure($"{user.Message} - check username / email");
            }

            // Update user data to request
            var updatedUserData = _mapper.Map(request, user.Data);
            updatedUserData!.Role = ValidateRole(request.Role);

            // Only update password if provided
            if (!string.IsNullOrEmpty(request.Password))
            {
                updatedUserData.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }
            var updatedUser = await _userRepository.UpdateUserAsync(updatedUserData!);
            var userResponse = _mapper.Map<UserResponse>(updatedUser);
            return Result<UserResponse>.Success(userResponse);
        }

        // Only Admin can activate the user
        public async Task<Result<UserResponse>> ActivateUser(int userId, int updateId)
        {
            // Access control 
            var authorisedToUpdate = await AuthorizedAction(updateId);
            if (!authorisedToUpdate.IsSuccess)
            {
               authorisedToUpdate = await AuthorizedAction(userId, updateId);
                if (!authorisedToUpdate.IsSuccess)
                {
                    return Result<UserResponse>.Failure($"{authorisedToUpdate.Message} - Failed to update");
                }
            }
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return Result<UserResponse>.Failure("User not found");
            }
            user.IsActive = true;

            var request = _mapper.Map<UserRequest>(user);
            return await UpdateUser(request);
            //var result = await _userRepository.UpdateUserAsync(user);
            //var response = _mapper.Map<UserResponse>(result);
            //return Result<UserResponse>.Success(response);
        }





















        /****************************************************
                  Need to work on the below methods
         ****************************************************/

        public async Task<Result<UserResponse>> AdminUserUpdate(int updateId, UserRequest request)
        {
            var user = await GetUser(request);
            if (user == null)
            {
                return Result<UserResponse>.Failure("User not found with provided username or email.");
            }
            var updatedUser = await UpdateUserAsync(request);
            return updatedUser;
        }

        public async Task<Result<UserResponse>> UserProfileUpdate(int userId, UserRequest request)
        {
            var user = await GetUser(request);
            if(user.Data?.Id != userId)
            {
                return Result<UserResponse>.Failure("You are not authorized to update the profile");
            }
            if (user == null)
            {
                return Result<UserResponse>.Failure("User not found.");
            }
            var updatedUser = await UpdateUserAsync(request);
            return updatedUser;
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

                var result = await _userRepository.DeleteUserAsync(existingUser.Data!);

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

                existingUser.Data!.IsActive = false;
                var result = await _userRepository.UpdateUserAsync(existingUser.Data!);
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
                if (accessingUser.Role == UserRole.Admin || id == accessedUserRecord.Data!.Id)
                {
                    return true;
                }
            }
            return false;
        }
    }
}