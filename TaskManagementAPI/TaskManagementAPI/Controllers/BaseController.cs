using Microsoft.AspNetCore.Mvc;
using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected int UserIdFromToken => User.GetUserIdFromToken();
        protected string UsernameFromToken => User.GetUsernameFromToken();
        protected string RoleFromToken => User.GetUserRoleFromToken();
    }
}
