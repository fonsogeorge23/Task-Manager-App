using Azure.Core;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Data;
using TaskManagementAPI.Models;
using LoginRequest = TaskManagementAPI.DTOs.Requests.LoginRequest;

namespace TaskManagementAPI.Repositories
{
    public interface IUserRepository
    {
        Task<User> RegisterUserAsync(User user, int createId);
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<IEnumerable<User>> GetAllActiveUsersAsync(bool? active = true);
        Task<User?> GetUserCredentialsAsync(LoginRequest request);
        Task<User?> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(User user);
    }
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User> RegisterUserAsync(User user, int createId)
        {
            if(createId > 0)
            {
                _context.OverrideUserId = createId;
            }
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if(createId == 0)
            {
                _context.Entry(user).State = EntityState.Modified;
                _context.OverrideUserId = user.Id;
                user.CreatedBy = user.Id;
                await _context.SaveChangesAsync();
            }
            return user;
        }
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserCredentialsAsync(LoginRequest request)
        {
            return await _context.Users.FirstOrDefaultAsync(u =>u.IsActive &&
                    (
                        EF.Functions.Collate(u.Username, "SQL_Latin1_General_CP1_CS_AS") == request.UsernameOrEmail ||
                        EF.Functions.Collate(u.Email, "SQL_Latin1_General_CP1_CS_AS") == request.UsernameOrEmail
                    ));
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<IEnumerable<User>> GetAllActiveUsersAsync(bool? active)
        {
            return await _context.Users
                .Where(u => u.IsActive == (active?? true)).ToListAsync();
        }

        public async Task<User?> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return await GetUserByIdAsync(user.Id);
        }

        public async Task<bool> DeleteUserAsync(User user)
        {
            _context.Users.Remove(user);
            var deleted = await _context.SaveChangesAsync();
            return deleted == 1? true: false;
        }
    }
}
