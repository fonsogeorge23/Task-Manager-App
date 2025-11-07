using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.DTOs.Responses;
using TaskManagementAPI.Services;
using LoginRequest = TaskManagementAPI.DTOs.Requests.LoginRequest;

namespace TaskManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : BaseController
    {
        // NOTE: The IUserService implementation needs to be updated with AuthenticateUserAsync.
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        #region REGISTER NEW USER
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRequest request)
        {
            var userResponse = await _userService.CreateUserAsync(request, UserIdFromToken);
            return HandleResult(userResponse);
        }
        #endregion

        #region USER LOGIN
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var authentication = await _userService.AuthenticateUser(request);
            return HandleResult(authentication);
        }
        #endregion






        /****************************************************
                  Need to work on the below methods
         ****************************************************/
        #region GET USERS
        [Authorize(Roles ="Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers(bool? active)
        {
            var userResponse = await _userService.GetAllUsers(UserIdFromToken, active);
            return HandleResult(userResponse);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int userId)
        {
            var userResponse = await _userService.GetActiveUserByIdAsync(UserIdFromToken, userId);
            return HandleResult(userResponse);
        }
        #endregion

        #region UPDATE USER PROFILE
        [Authorize]
        [HttpPatch("update-profile")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UserRequest request)
        {
            var userId = UserIdFromToken;
            if (RoleFromToken.Equals("Admin"))
            {
                var updateUser = await _userService.AdminUserUpdate(userId, request);
                return HandleResult(updateUser);
            }
            else
            {
                var updatedUserResponse = await _userService.UserProfileUpdate(userId, request);
                return HandleResult(updatedUserResponse);
            }
        }

        [Authorize]
        [HttpPatch("activate-users/{id}")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            var activatedUser = await _userService.ActivateUser(UserIdFromToken, id);
            return HandleResult(activatedUser);
        }
        #endregion

        #region DELETE USER
        [Authorize]
        [HttpDelete("delete-profile/{hardDelete}")]
        public async Task<IActionResult> DeleteProfile(bool hardDelete, UserRequest request)
        {
            var userId = UserIdFromToken;

            if (hardDelete)
            {
                var deletedUser = await _userService.HardDeleteUserAsync(userId, request);
                return HandleResult(deletedUser);
            }
            else
            {
                var inactivatedUser = await _userService.SoftDeleteUserAsync(userId, request);
                return HandleResult(inactivatedUser);
            }
        }
        #endregion

        #region DEBUG - GET USER INFO FROM TOKEN
        // ==============================================
        //   Debug endpoint to confirm token is working    
        // ==============================================
        [HttpGet("verify-token")]
        [Authorize]
        public IActionResult VerifyToken()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.FindFirstValue(ClaimTypes.Name);
            var role = User.FindFirstValue(ClaimTypes.Role);

            return Ok(new { userId, username, role });
        }
        #endregion
    }
}
