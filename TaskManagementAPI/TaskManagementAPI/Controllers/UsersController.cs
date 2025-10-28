using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.Services;
using TaskManagementAPI.Utilities;
using LoginRequest = TaskManagementAPI.DTOs.Requests.LoginRequest;

namespace TaskManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : BaseController
    {
        // NOTE: The IUserService implementation needs to be updated with AuthenticateUserAsync.
        private readonly IUserService _userService;
        private readonly IJwtAuthManager _jwtAuthManager;

        public UsersController(IUserService userService, IJwtAuthManager jwtAuthManager)
        {
            _userService = userService;
            _jwtAuthManager = jwtAuthManager;
        }

        // =====================
        // REGISTER NEW USER
        // =====================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username and password are required.");

            // Create user
            var userResponse = await _userService.CreateUserAsync(request);

            if(!userResponse.IsSuccess)
            {
                // Registration failed (e.g., username/email already exists)
                return BadRequest(userResponse.ErrorMessage);
            }
            // Returns 201 Created with the created user
            return CreatedAtAction(nameof(Register), new { id = userResponse.Data.Id }, userResponse);
        }

        // =========================
        // LOGIN AND GENERATE TOKEN 
        // =========================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // 1. Authenticate the user (call to the service layer to check credentials)
            var authenticatedUser = await _userService.AuthenticateUserAsync(request.Username, request.Password);

            if (!authenticatedUser.IsSuccess)
            {
                // Authentication failed (e.g., bad username or password)
                return Unauthorized(authenticatedUser.ErrorMessage);
            }

            // 2. Generate token using the authenticated user's details
            var token = _jwtAuthManager.GenerateToken(
                authenticatedUser.Data.Id,
                authenticatedUser.Data.UserName,
                authenticatedUser.Data.Role.ToString()
            );

            // 3. Return the token and user details
            return Ok(new { 
                Token = token, 
                Username = authenticatedUser.Data.UserName,
                authenticatedUser.Data.Email,
                authenticatedUser.Data.Role,
                });

        }

        // =========================
        // Get All Users (Admin Only)
        // =========================
        [Authorize(Roles ="Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            var usersResponse = await _userService.GetAllUsersAsync();
            return Ok(usersResponse);
        }

        // ===============
        // Get user by ID 
        // ===============
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var userResponse = await _userService.GetUserByIdAsync(id);
            if (!userResponse.IsSuccess)
            {
                return NotFound(userResponse);
            }
            return Ok(userResponse);
        }

        // Debug endpoint to confirm [Authorize] is working
        [Authorize(Roles = "Admin")]
        [HttpGet("debug-authorize")]
        public IActionResult DebugAuthorize()
        {
            // Get user identity from token
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.FindFirstValue(ClaimTypes.Name);
            var role = User.FindFirstValue(ClaimTypes.Role); 

            return Ok(new
            {
                Message = "Authorization successful!",
                UserIdFromToken = userId,
                UsernameFromToken = username,
                RoleFromToken = role
            });
        }
    }
}
