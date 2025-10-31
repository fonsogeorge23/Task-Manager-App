using Microsoft.AspNetCore.Mvc;
using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected int UserIdFromToken => User.GetTokenUserId();
        protected string UsernameFromToken => User.GetTokenUsername();
        protected string RoleFromToken => User.GetTokenRole();

        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if(result == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "No data found.",
                    timestamp = DateTime.UtcNow
                });
            }

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = result.Message ?? "Completed Successfully",
                    timestamp = result.Timestamp
                });
            }
            return BadRequest(new
            {
                success = false,
                message = result.Message,
                timestamp = result.Timestamp
            });
        }

        protected IActionResult HandleResult(Result result)
        {
            if(result == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "No data found.",
                    timestamp = DateTime.UtcNow
                });
            }
            if (result.IsSuccess)
            {
                return Ok(new
                {
                    success = true,
                    message = result.Message ?? "Completed Successfully",
                    timestamp = result.Timestamp
                });
            }
            return BadRequest(new
            {
                success = false,
                message = result.Message,
                timestamp = result.Timestamp
            });
        }
    }
}
