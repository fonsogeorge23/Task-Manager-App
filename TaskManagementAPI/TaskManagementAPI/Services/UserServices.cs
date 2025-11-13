using AutoMapper;
using Azure.Core;
using System.Collections.Generic;
using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.DTOs.Responses;
using TaskManagementAPI.Models;
using TaskManagementAPI.Repositories;
using TaskManagementAPI.Utilities; // Added using statement for Result<T>

namespace TaskManagementAPI.Services
{
    public interface IUserService
    {
        Task<Result<UserResponse>> ActivateUserService(UserRequest request, int userIdFromToken);

        // Method to authenticate user login request
        Task<Result<LoginResponse>> AuthenticateUserService(LoginRequest request);

        // Method to register a new user to the system
        Task<Result<UserResponse>> CreateUserService(UserRequest request);

        // Method for admin to get all user data, active or inactive
        Task<Result<IEnumerable<UserResponse>>> GetAllUsersService(bool? active, int userId);

        // Method to get user info (Admin can get inactive user info too)
        Task<Result<UserResponse>> GetUserService(UserRequest request, int userId);

        Task<Result<string>> HardDeleteUserService(UserRequest request, int accessId);

        Task<Result<UserResponse>> InactivateUserService(UserRequest request, int accessId);

        // Method to Update the user Service (Admin can update any user data)
        Task<Result<UserResponse>> UpdateUserService(UserRequest request, int userIdFromToken);
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

        public async Task<Result<UserResponse>> ActivateUserService(UserRequest request, int userIdFromToken)
        {
            var user = await GetUser(request);
            if (!user.IsSuccess || user.Data!.IsActive)
            {
                Result<UserResponse>.Failure($"{user.Message} - Failed to activate user");
            }
            user.Data!.IsActive = true;
            return await UpdateUser(user.Data);
        }

        // Method to authenticate user login request (mapping happening in public method)
        public async Task<Result<LoginResponse>> AuthenticateUserService(LoginRequest request)
        {
            // We validate the login request and get the user
            var validLoginUser = await ValidateLoginRequest(request);
            if (!validLoginUser.IsSuccess)
            {
                return Result<LoginResponse>.Failure($"{validLoginUser.Message} - Failed Login");
            }

            // returning the user data with token
            var response = _mapper.Map<UserResponse>(validLoginUser.Data);
            var token = await GenerateToken(validLoginUser.Data!);
            return Result<LoginResponse>.Success(
                new LoginResponse
                {
                    User = response,
                    Token = token.Data!
                }, $"{validLoginUser.Message} - Login successfully");
        }

        // Method to register a new user to the system
        public async Task<Result<UserResponse>> CreateUserService(UserRequest request)
        {
            // Validate the incoming user request
            var validRequest = await ValidateUserRequest(request);
            if (!validRequest.IsSuccess || string.IsNullOrWhiteSpace(validRequest.Message))
            {
                return Result<UserResponse>.Failure(validRequest.Message!);
            }

            // Save user to the database and return response
            return await RegisterUser(request);

        }

        // Method for admin to get all user data, active or inactive (mapping happening in public method)
        public async Task<Result<IEnumerable<UserResponse>>> GetAllUsersService(bool? active, int userIdFromToken)
        {
            // check admin user active or not
            var activeAdmin = await ValidUser(userIdFromToken);
            if(!activeAdmin.IsSuccess)
            {
                return Result<IEnumerable<UserResponse>>.Failure($"{activeAdmin.Message} - Failed to retrieve");
            }

            IEnumerable<UserResponse> usersList;
            // if active is set, return according to the value
            if (active!= null)
            {
                // if active is not set, we return all active and inactive users
                var activeList = await GetActiveUsers(active.Value);
                if (!activeList.IsSuccess)
                {
                    var activeMessage = active.Value ? "Active" : "Inactive";
                    Result<IEnumerable<UserResponse>>.Failure($"No {activeMessage} users exist");
                }
                usersList = _mapper.Map<IEnumerable<UserResponse>>(activeList.Data);
            }
            else
            {
                var allUsers = await GetAllUsers();
                if (!allUsers.IsSuccess)
                {
                    return Result<IEnumerable<UserResponse>>.Failure("No users exist");
                }
                usersList = _mapper.Map<IEnumerable<UserResponse>>(allUsers.Data);
            }

            // returning user for active value
            return Result<IEnumerable<UserResponse>>.Success(usersList);
        }

        // Method to get user info (Admin can get inactive user info too)
        public async Task<Result<UserResponse>> GetUserService(UserRequest request, int userId)
        {

            var validAccess = await ValidateAccessUser(request, userId);
            if (!validAccess.IsSuccess)
            {
                return Result<UserResponse>.Failure($"{validAccess.Message} - Failed to get user for request");
            }

            // returning the user info
            var userResult = await GetUser(request);
            if (!userResult.IsSuccess || userResult.Data == null)
            {
                return Result<UserResponse>.Failure($"{userResult.Message} - User not found");
            }

            // Map entity → DTO
            var response = _mapper.Map<UserResponse>(userResult.Data);

            // Return mapped response
            return Result<UserResponse>.Success(response);
        }

        public async Task<Result<string>> HardDeleteUserService(UserRequest request, int accessId)
        {
            var authorizedUser = await ValidateAccessUser(request, accessId);
            if (!authorizedUser.IsSuccess)
            {
                Result<string>.Failure($"{authorizedUser.Message} - Failed to delete user");
            }
            return await DeleteUser(request);
        }

        public async Task<Result<UserResponse>> InactivateUserService(UserRequest request, int userId)
        {
            var authorizedUser = await ValidateAccessUser(request, userId);
            if (!authorizedUser.IsSuccess)
            {
                Result<string>.Failure($"{authorizedUser.Message} - Failed to inactivate user");
            }
            return await InactivateUser(request);
        }

        // Method to Update the user Service (Admin can update any user data)
        public async Task<Result<UserResponse>> UpdateUserService(UserRequest request, int userId)
        {
            var authorizedUser = await ValidateAccessUser(request, userId);
            if (!authorizedUser.IsSuccess)
            {
                Result<string>.Failure($"{authorizedUser.Message} - Failed to update user");
            }

            return await UpdateUser(request);
        }

        /********************************************************
         *      private helper methods for public methods       *
         ********************************************************/

        // 1. ValidateUserRequest(UserRequest)  -> validating the incoming user request 
        // 2. UserExist(UserRequest)            -> check user info exist in DB
        // 3. ValidateRole(UserRequest)         -> validate the user request is having a valid role(default: 'Guest')
        // 4. GetHashCode(string)               -> get hash value for a string
        // 5. RegisterUser(user)                -> insert user to the database
        // 6. ValidateLoginRequest              -> validating the login request to return the user info
        // 7. GenerateToken                     -> generate jwt token using JwtAuthManager.cs
        // 8. ValidUser(userId)                 -> checks the user is active
        // 9. GetActiveUsers(active)            -> to get user with IsActive = {active} 
        //10. GetAllUsers                       -> to get all users in the DB
        //11. GetUser(UserRequest)              -> to get the user from user request(by username / email)
        //12. GetUser(User)                     -> to get the response from user object
        //12. GetUserByUsername                 -> using username from UserRequest to get user info
        //13. GetUserByEmail                    -> using email from UserRequest to get user info
        //15. ValidateAccessUser                -> validate the user accessing the info is authorized, if yes return the user info
        //16. UpdateUser(UserRequest)           -> update existing user data to the DB
        //17. DeleteUser(User)                  -> method to remove the user data from DB
        //18. InactivateUser(User)              -> method to inactivate the user
        /********************************************************/

        // Helper method to validate the user request
        private async Task<Result<UserRequest>> ValidateUserRequest(UserRequest request)
        {
            if (request == null)
                return Result<UserRequest>.Failure("Request cannot be null");

            var errors = new List<string>();

            // Mandatory fields
            if (string.IsNullOrWhiteSpace(request.Username))
                errors.Add("Username is required");

            if (string.IsNullOrWhiteSpace(request.Password))
                errors.Add("Password is required");

            if (string.IsNullOrWhiteSpace(request.Email))
                errors.Add("Email is required");

            // Optional role check — only validate if provided
            if (!string.IsNullOrWhiteSpace(request.Role))
                request.Role = ValidateRole(request.Role).ToString();

            // Return failure if any errors found
            if (errors.Any())
                return Result<UserRequest>.Failure(string.Join(" / ", errors));

            // If everything is valid
            return Result<UserRequest>.Success(request, "Valid registration request");
        }

        // Helper method to check user exist in system
        private async Task<Result> UserExist(UserRequest request)
        {
            var user = await GetUser(request);
            if (user.IsSuccess)
            {
                return Result.Success("Existing User");
            }
            return Result.Failure(user.Message!);
        }
        // Helper method to validate the role 
        private UserRole ValidateRole(string role)
        {
            if (!Enum.TryParse<UserRole>(role, true, out var parsedRole))
            {
                return UserRole.Guest;
            }
            return parsedRole;
        }

        // Helper method to generate Hash value for a string
        private Task<string> GetHashCode(string str)
        {
            return Task.FromResult(BCrypt.Net.BCrypt.HashPassword(str));
        }

        // Helper method to insert user to the DB
        private async Task<Result<UserResponse>> RegisterUser(UserRequest request)
        {
            // Checking user exist
            var userExist = await UserExist(request);
            if (userExist.IsSuccess)
            {
                var existingUser = _mapper.Map<UserResponse>(GetUser(request));
                return Result<UserResponse>.Success(existingUser, "User already exist");
            }

            // Map UserRequest to User entity
            var user = _mapper.Map<User>(request);
            user.PasswordHash = await GetHashCode(request.Password);
            var newUser = await _userRepository.RegisterUserAsync(user);
            if (newUser == null)
            {
                Result<UserResponse>.Failure("User not registered");
            }
            var response = _mapper.Map<UserResponse>(newUser);
            return Result<UserResponse>.Success(response, "User Registered successfully");
        }

        // Helper method to validate the incoming login request
        private async Task<Result<User>> ValidateLoginRequest(LoginRequest request)
        {
            if(string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Result<User>.Failure("Invalid login request");
            }
            var user = await GetUser(request);

            // returning only active user
            if (!user.IsSuccess)
            {
                return Result<User>.Failure($"{user.Message}");
            }
            return Result<User>.Success(user.Data!, "Authorized");
        }

        // Helper method to get active user from LoginRequest
        private async Task<Result<User>> GetUser(LoginRequest loginRequest)
        {
            var user = await GetUserByUsername(loginRequest.Username);
            // only accepting active user to login
            if (!user.IsSuccess || !user.Data!.IsActive)
            {
                return Result<User>.Failure("No user found / Inactive");
            }
            return user;
        }

        // Helper method to generate token for the login user claims
        private async Task<Result<string>> GenerateToken(User user)
        {
            var token = await _jwtAuthManager.GenerateToken(user);            
            return Result<string>.Success(token);
        }

        // Helper method to validate a active user
        private async Task<Result> ValidUser(int userId)
        {
            var validUser = await GetUserById(userId);
            if (!validUser.IsSuccess || !validUser.Data!.IsActive)
            {
                return Result.Failure($"{validUser.Message} / Inactive");
            }
            return Result.Success();
        }

        // Helper method to get user info by Id
        private async Task<Result<User>> GetUserById(int id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if(user == null)
            {
                Result<User>.Failure("User not exist");
            }
            return Result<User>.Success(user!);
        }

        // Helper method to get users according to IsActive value
        private async Task<Result<IEnumerable<User>>> GetActiveUsers(bool active)
        {
            var users = await _userRepository.GetAllActiveUsersAsync(active);
            return Result<IEnumerable<User>>.Success(users);
        }

        // Helper method to get all users
        private async Task<Result<IEnumerable<User>>> GetAllUsers()
        {
            var user = await _userRepository.GetAllUsersAsync(); 
            return Result<IEnumerable<User>>.Success(user);
        }

        // Helper method to get user from UserRequest
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
            return Result<User>.Success(user.Data!, "User exist");
        }

        // Helper method to get user response from User
        private async Task<Result<UserResponse>> GetUser(User user)
        {
            var response = _mapper.Map<UserResponse>(user);
            return Result<UserResponse>.Success(response);
        }
        // Helper method to get the user by username
        private async Task<Result<User>> GetUserByUsername(string username)
        {
            var user = await _userRepository.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return Result<User>.Failure("User not found");
            }
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
            return Result<User>.Success(user);
        }

        // Helper method to validate the user accessing the info is authorized
        private async Task<Result> ValidateAccessUser(UserRequest request, int userId)
        {
            var user = await GetUser(request);

            // if the access user is active and existing
            if (user.IsSuccess)
            {
                var adminUser = await GetUserById(userId);
                // if user is accessing his own info or "Admin" is access the info
                if((userId == user.Data!.Id && user.Data!.IsActive) || adminUser.Data!.Role == UserRole.Admin)
                {
                    return Result.Success("Authorized");
                }
            }
            return Result.Failure("Unauthorized");
        }

        // Helper method to 
        private async Task<Result<UserResponse>> UpdateUser(UserRequest request)
        {
            var user = await GetUser(request);
            if (!user.IsSuccess)
            {
                return Result<UserResponse>.Failure(user.Message!);
            }

            // Mapping request data to user
            _mapper.Map(request, user);
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.Data!.PasswordHash = await GetHashCode(request.Password);
            }
            return await UpdateUser(user.Data!);
        }
        
        // Helper method to update the user data in DB
        private async Task<Result<UserResponse>> UpdateUser(User user)
        {
            var updatedUser = await _userRepository.UpdateUserAsync(user);
            var response = _mapper.Map<UserResponse>(updatedUser);
            return Result<UserResponse>.Success(response);
        }

        // Helper method to remove the user record
        private async Task<Result<string>> DeleteUser(UserRequest request)
        {
            var user = await GetUser(request);
            _mapper.Map(request, user.Data);
            var deletedUser = await _userRepository.DeleteUserAsync(user.Data!);
            if (!deletedUser)
            {
                Result<string>.Failure("Failed to delete");
            }
            return Result<string>.Success("User deleted successfully");
        }

        // Helper method to inactivate the user
        private async Task<Result<UserResponse>> InactivateUser(UserRequest request)
        {
            var user = await GetUser(request);
            if (!user.Data!.IsActive)
            {
                Result<UserResponse>.Failure("User already active");
            }
            _mapper.Map(request, user.Data);
            user.Data!.IsActive = false;
            return await UpdateUser(user.Data!);
        }
    }
}