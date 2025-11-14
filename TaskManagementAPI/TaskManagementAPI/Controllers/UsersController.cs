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
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        #region REGISTER NEW USER
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(UserRequest request)
        {
            int accessUserId = 0; // Default: no token (public registration)

            if (User.Identity?.IsAuthenticated == true)
            {
                accessUserId = UserIdFromToken; // Admin token only
            }
            var userResponse = await _userService.CreateUserService(request, accessUserId);
            return HandleResult(userResponse);
        }
        #endregion

        #region USER LOGIN
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var authentication = await _userService.AuthenticateUserService(request);
            return HandleResult(authentication);
        }
        #endregion

        #region GET USERS
        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers(bool? active)
        {
            var userResponse = await _userService.GetAllUsersService(active, UserIdFromToken);
            return HandleResult(userResponse);
        }

        [Authorize]
        [HttpPost("user/{userId}")]
        public async Task<IActionResult> GetUser(int userId)
        {
            var userResponse = await _userService.GetUserService(userId, UserIdFromToken);
            return HandleResult(userResponse);
        }
        #endregion

        #region UPDATE USER PROFILE
        [Authorize]
        [HttpPatch("update-profile/{userId}")]
        public async Task<IActionResult> UpdateUserProfile(int userId, [FromBody] UserRequest request)
        {
            var updateUser = await _userService.UpdateUserService(userId, request, UserIdFromToken);
            return HandleResult(updateUser);
        }

        //[Authorize]
        //[HttpPatch("activate-users")]
        //public async Task<IActionResult> ActivateUser(UserRequest request)
        //{
        //    var activatedUser = await _userService.ActivateUserService(request, UserIdFromToken);
        //    return HandleResult(activatedUser);
        //}
        #endregion

        //#region DELETE/INACTIVATE USER
        //[Authorize]
        //[HttpDelete("delete-profile/{hardDelete}")]
        //public async Task<IActionResult> DeleteProfile(UserRequest request, bool hardDelete)
        //{
        //    var userId = UserIdFromToken;

        //    if (hardDelete)
        //    {
        //        var deletedUser = await _userService.HardDeleteUserService(request, userId);
        //        return HandleResult(deletedUser);
        //    }
        //    else
        //    {
        //        var inactivatedUser = await _userService.InactivateUserService(request, userId);
        //        return HandleResult(inactivatedUser);
        //    }
        //}
        //#endregion

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
