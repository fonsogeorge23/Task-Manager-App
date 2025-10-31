using System.Security.Claims;

namespace TaskManagementAPI.Utilities
{
    public static class ControllerExtensions
    {
        public static int GetTokenUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null)
                throw new UnauthorizedAccessException("User ID not found in token.");

            return int.TryParse(userIdClaim, out var userId)?userId:0;
        }

        public static string GetTokenUsername(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Name)?.Value ?? throw new UnauthorizedAccessException("Username not found in token.");
        }

        public static string GetTokenRole(this ClaimsPrincipal user)
        {
            //return user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            return string.IsNullOrEmpty(role) ? "Guest" : role;
        }
    }
}
