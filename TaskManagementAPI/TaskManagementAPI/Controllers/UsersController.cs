using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.DTOs.Responses;
using TaskManagementAPI.Repositories.Interfaces;
using TaskManagementAPI.Services.Interfaces;
using LoginRequest = TaskManagementAPI.DTOs.Requests.LoginRequest;

namespace TaskManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        private readonly IUserRepository _userRepository;

        public UsersController(IUserService userService, IJwtService jwtService, IUserRepository userRepository)
        {
            _userService = userService;
            _jwtService = jwtService;
            _userRepository = userRepository;
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

        // =====================
        // LOGIN USER
        // =====================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Get user entity from DB (to verify password)
            var userEntity = await _userRepository.GetUserByUsernameAsync(request.Username);
            if (userEntity == null)
                return Unauthorized("Invalid username or password");

            // Verify password
            bool valid = BCrypt.Net.BCrypt.Verify(request.Password, userEntity.PasswordHash);
            if (!valid)
                return Unauthorized("Invalid username or password");

            // Generate JWT token
            var token = _jwtService.GenerateToken(
                userEntity.Id,
                userEntity.Username,
                userEntity.Role.ToString()
            );

            // Map User entity to UserResponse DTO
            var userResponse = new UserResponse
            {
                Id = userEntity.Id,
                UserName = userEntity.Username,
                Email = userEntity.Email,
                Role = userEntity.Role
            };

            // Return token + user info
            return Ok(new { Token = token, User = userResponse });
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
