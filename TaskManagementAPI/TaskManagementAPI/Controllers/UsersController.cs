using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManagementAPI.Data;
using TaskManagementAPI.Models;
using TaskManagementAPI.Models.DTOs;

namespace TaskManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/users
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody]UserCreateDto user)
        {
            var entity = new User
            {
                Username = user.UserName,
                Email = user.Email,
                PasswordHash = user.Password,
                Role = user.Role
            };

            _context.Users.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = entity.Id }, entity);
        }

        // GET: api/users/1
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return Ok(user);
        }
    }
}
