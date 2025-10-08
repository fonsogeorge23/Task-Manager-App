using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace TaskManagementAPI.Static
{
    public interface IJwtAuthManager
    {
        string GenerateToken(int userId, string username, string role);
    }
    public class JwtAuthManager: IJwtAuthManager
    {
        private readonly string _key;

        public JwtAuthManager(string key)
        {
            _key = key;
        }

        public string GenerateToken(int userId, string username, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenKey = Encoding.UTF8.GetBytes(_key);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    // 1. ClaimTypes.NameIdentifier: Stores the User ID (Essential for secure identification)
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    
                    // 2. ClaimTypes.Name: Stores the Username
                    new Claim(ClaimTypes.Name, username),

                    // 3. ClaimTypes.Role: Stores the User Role
                    new Claim(ClaimTypes.Role, role)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
