using System.Security.Claims;

namespace TaskManagementAPI.Utilities
{
    public static class ControllerExtensions
    {
        public static int GetUserIdFromToken(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                throw new UnauthorizedAccessException("User ID not found in token.");

            return int.Parse(userIdClaim.Value);
        }

        public static string GetUsernameFromToken(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Name)?.Value ?? throw new UnauthorizedAccessException("Username not found in token.");
        }

        public static string GetUserRoleFromToken(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }
    }
}
