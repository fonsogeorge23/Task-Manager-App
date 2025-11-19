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

        public static UserRole GetTokenRole(this ClaimsPrincipal user)
        {
            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrWhiteSpace(role))
                return UserRole.Guest;
            // Try to parse (case-insensitive)
            if (Enum.TryParse<UserRole>(role, true, out var parsedRole))
                return parsedRole;
            // If parsing fails (invalid token role), also fallback
            return UserRole.Guest;
        }
    }
}
