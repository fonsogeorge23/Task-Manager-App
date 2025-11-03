using Azure.Core;
using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Data;
using TaskManagementAPI.Models;

namespace TaskManagementAPI.Repositories
{
    public interface IUserRepository
    {
        Task<User> RegisterUserAsync(User user);
        Task<User?> GetActiveUserByIdAsync(int id);
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<IEnumerable<User>> GetAllActiveUsersAsync();
        Task<User?> GetUserCredentialsAsync(string username, string password);
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

        public async Task<User> RegisterUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
        
        public async Task<User?> GetActiveUserByIdAsync(int id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
        }
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Email== email);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserCredentialsAsync(string username, string password)
        {
            return await _context.Users.FirstOrDefaultAsync(u =>
            EF.Functions.Collate(u.Username, "SQL_Latin1_General_CP1_CS_AS") == username );
        }

        public async Task<IEnumerable<User>> GetAllActiveUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users.IgnoreQueryFilters().ToListAsync();
        }
        public async Task<User?> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return await _context.Users.IgnoreQueryFilters<User>().FirstOrDefaultAsync(u => u.Id == user.Id);
        }

        public async Task<bool> DeleteUserAsync(User user)
        {
            _context.Users.Remove(user);
            var deleted = await _context.SaveChangesAsync();
            return deleted == 1? true: false;
        }
    }
}
