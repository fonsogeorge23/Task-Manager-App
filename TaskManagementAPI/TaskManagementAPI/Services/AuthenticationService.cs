using AutoMapper;
using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.DTOs.Responses;
using TaskManagementAPI.Models;
using TaskManagementAPI.Repositories;
using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.Services
{
    public interface IAuthenticationService
    {
        // Method to authenticate user login request
        Task<Result<LoginResponse>> AuthenticateLoginUser(LoginRequest request);
    }
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserService _userService;
        private readonly IJwtAuthManager _jwtAuthManager;
        private readonly IMapper _mapper;

        public AuthenticationService(IUserService userService, IJwtAuthManager jwtAuthManager, IMapper mapper)
        {
            _userService = userService;
            _jwtAuthManager = jwtAuthManager;
            _mapper = mapper;
        }

        // Method to authenticate user login request 
        public async Task<Result<LoginResponse>> AuthenticateLoginUser(LoginRequest request)
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


        /********************************************************
         *      private helper methods for public methods       *
         ********************************************************/
        // 01. ValidateLoginRequest(UserRequest)			-> validating the incoming user request
        // 02. VerifyPassword(Password, PasswordHash)		-> verify the incoming password with the system record
        // 03. GenerateToken(user)							-> generate jwt token using JwtAuthManager.cs     
        //********************************************************/

        // Helper method to validate the incoming login request
        private async Task<Result<User>> ValidateLoginRequest(LoginRequest request)
        {
            bool emptyIdentifier = string.IsNullOrWhiteSpace(request.UsernameOrEmail);
            bool emptyPassword = string.IsNullOrWhiteSpace(request.Password);
            if (emptyIdentifier || emptyPassword)
            {
                return Result<User>.Failure("Invalid login request");
            }

            // returning only active user
            var user = await _userService.GetUser(request);
            if (!user.IsSuccess)
            {
                return Result<User>.Failure($"{user.Message}");
            }

            var validPassword = VerifyPassword(request.Password, user.Data!.PasswordHash);

            return validPassword ?
                Result<User>.Success(user.Data!, "Authorized") :
                Result<User>.Failure("Check credentials");
        }

        // Helper method to generate token for the login user claims
        private async Task<Result<string>> GenerateToken(User user)
        {
            var token = await _jwtAuthManager.GenerateToken(user);
            return Result<string>.Success(token);
        }

        // Helper method to verify the password for login
        private bool VerifyPassword(string plainPassword, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
        }

    }
}
