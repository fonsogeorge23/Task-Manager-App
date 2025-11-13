using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskManagementAPI.Models;

namespace TaskManagementAPI.Utilities
{
    public interface IJwtAuthManager
    {/// <summary>
     /// Generates a JWT token asynchronously.
     /// </summary>
     /// <param name="userId">The ID of the user.</param>
     /// <param name="username">The username of the user.</param>
     /// <param name="role">The role of the user.</param>
     /// <returns>A Task that represents the asynchronous operation, yielding the generated JWT token string.</returns>
        Task<string> GenerateToken(User user);
    }

    public class JwtAuthManager : IJwtAuthManager
    {
        private readonly string _key;
        private readonly JwtSettings _jwtSettings;

        public JwtAuthManager(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
            _key = _jwtSettings.SecretKey;
        }

        /// <summary>
        /// Generates a JWT token synchronously and wraps the result in a Task to satisfy the async interface.
        /// </summary>
        public Task<string> GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenKey = Encoding.UTF8.GetBytes(_key);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(_jwtSettings.ExpiryInHours),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,

                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(tokenKey),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            string tokenString = tokenHandler.WriteToken(token);

            // Since token generation is a fast CPU-bound operation, we use Task.FromResult
            // to return the value as a completed Task, satisfying the async interface.
            return Task.FromResult(tokenString);
        }
    }
}
