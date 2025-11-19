using AutoMapper;
using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.DTOs.Responses;
using TaskManagementAPI.Models;
using TaskManagementAPI.Repositories;
using TaskManagementAPI.Utilities; // Added using statement for Result<T>

namespace TaskManagementAPI.Services
{
    public interface IUserService
    {
        // Method to activate an inactive user by Admin
        Task<Result<UserResponse>> ActivateUserService(int userId, int accessId);

        // Method to authenticate user login request
        Task<Result<LoginResponse>> AuthenticateUserService(LoginRequest request);

        // Method to register a new user to the system
        Task<Result<UserResponse>> CreateUserService(UserRequest request, int createUserId);

        // Method for admin to get all user data, active or inactive
        Task<Result<IEnumerable<UserResponse>>> GetAllUsersService(bool? active, int accessId);

        // Method to get user info (Admin can get inactive user info too)
        Task<Result<UserResponse>> GetUserService(int userId, int accesssId);

        // Method to remove a user from system by Admin
        Task<Result<string>> HardDeleteUserService(int userId, int accessId);

        // Method to inactivate a user
        Task<Result<UserResponse>> InactivateUserService(int userId, int accessId);

        // Method to Update the user Service (Admin can update any user data)
        Task<Result<UserResponse>> UpdateUserService(int targetUserId, UserRequest request, int userIdFromToken);
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

        // Method to activate an inactive user by Admin
        public async Task<Result<UserResponse>> ActivateUserService(int userId, int accessId)
        {
            // Actor must be admin
            var actor = await GetActiveUser(accessId);
            if (!actor.IsSuccess)
                return Result<UserResponse>.Failure("Only active admin can activate a user.");

            var user = await GetUserById(userId);
            if (!user.IsSuccess)
            {
                return Result<UserResponse>.Failure($"{user.Message} - User activation failed");
            }

            // If user already active
            if (user.Data!.IsActive)
            {
                return Result<UserResponse>.Failure("User already active");
            }

            user.Data!.IsActive = true;
            return await UpdateUserCore(user.Data);
        }

        // Method to authenticate user login request 
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
        public async Task<Result<UserResponse>> CreateUserService(UserRequest request, int createUserId)
        {
            // Validate the incoming user request
            var validRequest = await ValidateUserRequest(request);
            if (!validRequest.IsSuccess)
            {
                return Result<UserResponse>.Failure(validRequest.Message!);
            }

            // Save user to the database and return response
            return await RegisterUser(request, createUserId);

        }

        // Method for admin to get all user data, active or inactive 
        public async Task<Result<IEnumerable<UserResponse>>> GetAllUsersService(bool? active, int accessId)
        {
            // check admin user active or not
            var activeAdmin = await GetActiveUser(accessId);
            if(!activeAdmin.IsSuccess)
            {
                return Result<IEnumerable<UserResponse>>.Failure($"{activeAdmin.Message} - Failed to retrieve");
            }

            IEnumerable<UserResponse> usersList;
            // if active is set, return according to the value
            if (active!= null)
            {
                // if active is not set, we return all active and inactive users
                var activeList = await GetAllActiveUsers(active.Value);
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
        public async Task<Result<UserResponse>> GetUserService(int userId, int accesssId)
        {

            var validAccess = await ValidateAccessUser(userId, accesssId);
            if (!validAccess.IsSuccess)
            {
                return Result<UserResponse>.Failure($"{validAccess.Message} - Failed to get user for request");
            }

            // returning the user info
            var userResult = await GetUserById(userId);
            if (!userResult.IsSuccess || userResult.Data == null)
            {
                return Result<UserResponse>.Failure($"{userResult.Message} - User not found");
            }

            // Map entity → DTO
            var response = _mapper.Map<UserResponse>(userResult.Data);

            // Return mapped response
            return Result<UserResponse>.Success(response);
        }

        // Method to remove a user from system by Admin
        public async Task<Result<string>> HardDeleteUserService(int userId, int accessId)
        {
            // Only an active admin can remove user from DB
            var authorizedUser = await ValidateAccessUser(userId, accessId);
            if (!authorizedUser.IsSuccess)
            {
                return Result<String>.Failure($"{authorizedUser.Message} - Failed to delete");
            }

            // if authorized, remove the data from DB
            return await DeleteUser(userId);
        }

        // Method to inactivate a user 
        public async Task<Result<UserResponse>> InactivateUserService(int userId, int accessId)
        {
            var authorizedUser = await ValidateAccessUser(userId, accessId);
            if (!authorizedUser.IsSuccess)
            {
                Result<string>.Failure($"{authorizedUser.Message} - Failed to inactivate user");
            }

            return await InactivateUser(userId);
        }

        // Method to Update the user Service (Admin can update any user data)
        public async Task<Result<UserResponse>> UpdateUserService(int targetUserId, UserRequest request, int accessId)
        {
            //throw new NotImplementedException();
            // Check for authorized user info access
            var access = await ValidateAccessUser(targetUserId, accessId);
            if (!access.IsSuccess)
                return Result<UserResponse>.Failure($"{access.Message} - Failed to update user");

            return await UpdateUser(request, targetUserId, accessId);
        }


        /********************************************************
         *      private helper methods for public methods       *
         ********************************************************/

        // 01. ValidateUserRequest(request)					-> validating the incoming user request
        // 02. GetUser(request)								-> to get the user from user request(by username / email)
        // 03. GetUserByUsername(username)					-> using username from UserRequest to get user info
        // 04. GetUserByEmail(email)						-> using email from UserRequest to get user info
        // 05. RegisterUser(request, createUserId)			-> insert user to the database
        // 06. GetHashCode(string)							-> get hash value for a string
        // 07. ValidateRole(role)							-> validate the user request is having a valid role(default: 'Guest')
        // 08. GetUserById(userId)							-> get user by userId 	
        // 09. ValidateLoginRequest(request)				-> validating the login request to return the user info
        // 10. GetUser(LoginRequest)                        -> get user info with login request
        // 11. VerifyPassword(Password, PasswordHash)		-> verify the incoming password with the system record
        // 12. GenerateToken(user)							-> generate jwt token using JwtAuthManager.cs
        // 13. GetActiveUser(userId)						-> get a user info using userId
        // 14. GetAllActiveUsers(active.Value)				-> get all users by the boolean active value
        // 15. GetAllUsers()								-> get all users in the system for admin
        // 16. ValidateAccessUser(userId, accesssId)		-> validate the user accessing the info is authorized, if yes return the user info
        // 17. UpdateUser(request, targetUserId, accessId)	-> method to validate the request to update a user info
        // 18. ValidateUsername(username)					-> method to validate Username entering is existing or not 
        // 19. ValidateEmail(email)							-> method to validate email entering is existing or not
        // 20. UpdateUserCore(user)							-> method to update the user info in DB
        // 21. DeleteUser(userId)							-> method to remove the user data from DB
        // 22. InactivateUser(userId)						-> method to inactivate the user
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

            // Return failure if any errors found
            if (errors.Any())
                return Result<UserRequest>.Failure(string.Join(" / ", errors));


            // Checking user exist
            var existingUser = await GetUser(request);
            if (existingUser.IsSuccess)
            {
                return Result<UserRequest>.Failure("User already exist");
            }
            // If everything is valid
            return Result<UserRequest>.Success(request, "Valid registration request");
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
        
        // Helper method to insert user to the DB
        private async Task<Result<UserResponse>> RegisterUser(UserRequest request, int createUserId)
        {
            // Map UserRequest to User entity
            var user = _mapper.Map<User>(request);
            user.PasswordHash = await GetHashCode(request.Password);

            // checking if "Admin" is being created, only "Admin" can create another "Admin"
            var userRole = ValidateRole(request.Role);
            if (userRole == UserRole.Admin)
            {
                if (createUserId > 0)
                {
                    var adminUser = await GetUserById(createUserId);
                    if (adminUser.Data!.Role == UserRole.Admin)
                    {
                        userRole = UserRole.Admin;
                    }
                }
                else
                {
                    userRole = UserRole.User;
                }
            }
            user.Role = userRole;

            var newUser = await _userRepository.RegisterUserAsync(user);
            if (newUser == null)
            {
                Result<UserResponse>.Failure("User not registered");
            }
            var response = _mapper.Map<UserResponse>(newUser);
            return Result<UserResponse>.Success(response, "User Registered successfully");
        }

        // Helper method to generate Hash value for a string
        private Task<string> GetHashCode(string str)
        {
            return Task.FromResult(BCrypt.Net.BCrypt.HashPassword(str));
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

        // Helper method to get user info by Id
        private async Task<Result<User>> GetUserById(int id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null)
            {
                return Result<User>.Failure("User not exist");
            }
            return Result<User>.Success(user!, "User exist");
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

            var validPassword = VerifyPassword(request.Password, user.Data!.PasswordHash);

            return validPassword ?
                Result<User>.Success(user.Data!, "Authorized"):
                Result<User>.Failure("Check credentials");
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

        // Helper method to verify the password for login
        private bool VerifyPassword(string plainPassword, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
        }
        
        // Helper method to generate token for the login user claims
        private async Task<Result<string>> GenerateToken(User user)
        {
            var token = await _jwtAuthManager.GenerateToken(user);            
            return Result<string>.Success(token);
        }

        // Helper method to validate a active user
        private async Task<Result<User>> GetActiveUser(int userId)
        {
            var activeUser = await GetUserById(userId);
            if (!activeUser.IsSuccess || !activeUser.Data!.IsActive)
            {
                return Result<User>.Failure($"{activeUser.Message} - Unauthorized / Inactive");
            }
            return activeUser;
        }

        // Helper method to get users according to IsActive value
        private async Task<Result<IEnumerable<User>>> GetAllActiveUsers(bool active)
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

        // Helper method to validate the user accessing the info is authorized
        private async Task<Result> ValidateAccessUser(int targetUserId, int accessUserId)
        {
            if(targetUserId == 0 || accessUserId == 0)
                return Result.Failure("User Id cannot be 0");

            // If users is active or not
            var currentUser = await GetActiveUser(accessUserId);
            var targetUser = await GetUserById(targetUserId);

            if (!currentUser.IsSuccess)
                return Result.Failure($"{currentUser.Message} - Access Denied");

            if (!targetUser.IsSuccess)
                return Result.Failure("Target user not found");

            // If the user accessing info is an "Admin" or their own info
            if (currentUser.Data!.Role == UserRole.Admin)
                return Result.Success();

            if (targetUser.Data!.Id == accessUserId)
                return Result.Success();

            return Result.Failure("Unauthorized");
        }

        // Helper method to update user info from request
        private async Task<Result<UserResponse>> UpdateUser(UserRequest request, int userId, int accessId)
        {
            var user = await GetUserById(userId);
            if (!user.IsSuccess)
                return Result<UserResponse>.Failure($"{user.Message!} - target user not found");

            var accessUser = await GetUserById(accessId);

            if (!accessUser.IsSuccess)
                return Result<UserResponse>.Failure($"{user.Message!} - Access user not found");

            var entity = user.Data!;
            var actor = accessUser.Data!;

            // Mapping request data to user

            // Update Username
            if (!string.IsNullOrWhiteSpace(request.Username) &&
                request.Username != entity.Username)
            {
                var username = await ValidateUsername(request.Username);
                if (!username.IsSuccess)
                {
                    return Result<UserResponse>.Failure($"{username} is invalid.");
                }
                entity.Username = username.Data!;
            }

            // Update Email
            if (!string.IsNullOrWhiteSpace(request.Email) &&
                request.Email != entity.Email)
            {
                var email = await ValidateEmail(request.Email);
                if (!email.IsSuccess)
                {
                    return Result<UserResponse>.Failure($"{email} is invalid.");
                }
                entity.Email = email.Data!;
            }

            // Update Password
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                entity.PasswordHash = await GetHashCode(request.Password);
            }

            // Update Role - only admin can create another admin
            if (!string.IsNullOrWhiteSpace(request.Role) &&
                request.Role != entity.Role.ToString())
            {
                var requestedRole = ValidateRole(request.Role);

                // If the actor is Admin → assign whatever they asked
                if (actor.Role == UserRole.Admin)
                {
                    entity.Role = requestedRole;
                }
                else
                {
                    // Non-admins cannot upgrade to Admin
                    if (requestedRole == UserRole.Admin)
                    {
                        entity.Role = UserRole.User;   // force safe fallback
                    }
                    else
                    {
                        entity.Role = requestedRole;   // allow change
                    }
                }

            }

            return await UpdateUserCore(entity);
        }

        // Helper method to validate Username entering is existing or not
        private async Task<Result<string>> ValidateUsername(string username)
        {
            var user = await GetUserByUsername(username);
            if (user.IsSuccess)
            {
                return Result<string>.Failure("Username already exist");
            }
            return Result<string>.Success(username);
        }

        // Helper method to validate email entering is existing or not
        private async Task<Result<string>> ValidateEmail(string email)
        {
            var user = await GetUserByEmail(email);
            if (user.IsSuccess)
            {
                Result<string>.Failure("email already exist");
            }
            return Result<string>.Success(email);
        }

        // Helper method to update the user data in DB
        private async Task<Result<UserResponse>> UpdateUserCore(User user)
        {
            var updatedUser = await _userRepository.UpdateUserAsync(user);
            var response = _mapper.Map<UserResponse>(updatedUser);
            return Result<UserResponse>.Success(response, "User data updated successfully");
        }

        // Helper method to remove the user record
        private async Task<Result<string>> DeleteUser(int userId)
        {
            var user = await GetUserById(userId);
            if (!user.IsSuccess)
            {
                return Result<string>.Failure($"{user.Message} - Failed to delete");
            }

            var deletedUser = await _userRepository.DeleteUserAsync(user.Data!);
            if (!deletedUser)
            {
                Result<string>.Failure("Failed to delete");
            }
            return Result<string>.Success("User deleted successfully");
        }

        // Helper method to inactivate the user
        private async Task<Result<UserResponse>> InactivateUser(int userId)
        {
            var user = await GetActiveUser(userId);
            if(!user.IsSuccess)
            {
                return Result<UserResponse>.Failure($"{user.Message} - Failed to inactivate user");
            }
            user.Data!.IsActive = false;
            return await UpdateUserCore(user.Data!);
        }
    }
}