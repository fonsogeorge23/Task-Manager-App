using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.DTOs.Responses;
using TaskManagementAPI.Repositories.Interfaces;
using TaskManagementAPI.Services.Interfaces;
using TaskManagementAPI.Static;
using LoginRequest = TaskManagementAPI.DTOs.Requests.LoginRequest;

namespace TaskManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserRepository _userRepository;
        private readonly IJwtAuthManager _jwtAuthManager;

        public UsersController(IUserService userService, IUserRepository userRepository, IJwtAuthManager jwtAuthManager)
        {
            _userService = userService;
            _userRepository = userRepository;
            _jwtAuthManager = jwtAuthManager;
        }

        // =====================
        // REGISTER NEW USER
        // =====================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserCreateRequest request)
        {
            // Check if username already exists
            var existingUser = await _userRepository.GetUserByUsernameAsync(request.Username);
            if (existingUser != null)
                return BadRequest("Username already exists");

            // Create user
            var userResponse = await _userService.CreateUserAsync(request);

            // Returns 201 Created with the created user
            return CreatedAtAction(nameof(Register), new { id = userResponse.Id }, userResponse);
        }

        // Public endpoint to generate token for testing
        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
        {
            // 1. Authenticate the user (call to the service layer to check credentials)
            // NOTE: This assumes the existence of: Task<AuthenticatedUserResponse> AuthenticateUserAsync(string username, string password)
            // in IUserService and UserServices.
            var authenticatedUser = await _userService.AuthenticateUserAsync(request.Username, request.Password);

            if (authenticatedUser == null)
            {
                // Authentication failed (e.g., bad username or password)
                return Unauthorized("Invalid username or password.");
            }

            // 2. Generate token using the authenticated user's details
            var token = _jwtAuthManager.GenerateToken(
                authenticatedUser.Id,
                authenticatedUser.Username,
                authenticatedUser.Role.ToString()
            );

            return Ok(new { Token = token, Username = authenticatedUser.Username, Role = authenticatedUser.Role });
        }

        // Debug endpoint to confirm [Authorize] is working
        [Authorize(Roles ="Admin")]
        [HttpGet("debug-authorize")]
        public IActionResult DebugAuthorize()
        {
            // Get user identity from token
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.FindFirstValue(ClaimTypes.Name);
            var role = User.FindFirstValue(ClaimTypes.Role); // Now we can retrieve the role

            return Ok(new
            {
                Message = "Authorization successful!",
                UserIdFromToken = userId,
                UsernameFromToken = username,
                RoleFromToken = role
            });
        }

        [Authorize(Roles ="Admin")]
        [HttpGet("profile")]
        public IActionResult Profile()
        {
            // Extract claims from the JWT token
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var username = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(JwtRegisteredClaimNames.UniqueName);
            var role = User.FindFirstValue(ClaimTypes.Role);
            
            return Ok(new { 
                Message = "This is a protected profile endpoint.",
                UserId = userId, 
                Username = username, 
                Role = role } );
        }
    }
}
