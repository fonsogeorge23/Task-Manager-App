using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Data;
using TaskManagementAPI.Models;

namespace TaskManagementAPI.Repositories
{
    public interface ITaskRepository
    {
        Task<TaskObject> AddTaskAsync(TaskObject task);
        Task<IEnumerable<TaskObject>> GetActiveTasksItemsByUserIdAsync(int userId);
        Task<IEnumerable<TaskObject>> GetAllActiveTaskByStatus(int userId, string status);
        Task<TaskObject?> GetActiveTaskByTaskIdUserIdAsync(int id, int userId);
        Task<TaskObject?> GetActiveTaskByIdAsync(int id);
        Task<TaskObject?> GetTaskByTaskIdUserIdAsync(int taskId, int userId);
        Task<TaskObject?> GetTaskByIdAsync(int id);
        Task<TaskObject> UpdateTaskAsync(TaskObject task);
        Task<bool> InactivateTaskAsync(TaskObject task);
        Task<bool> ReactivateTaskByIdAsync(int id);
        Task<bool> DeleteTaskAsync(TaskObject task);

    }
    public class TaskRepository : ITaskRepository
    {
        private readonly AppDbContext _context;
        public TaskRepository(AppDbContext context)
        {
            _context = context;
        }

        // Method to add a new task
        public async Task<TaskObject> AddTaskAsync(TaskObject task)
        {
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<TaskObject?> GetActiveTaskByTaskIdUserIdAsync(int id, int userId)
        {
            return await _context.Tasks
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        }

        public async Task<TaskObject?> GetActiveTaskByIdAsync(int id)
        {
            return await _context.Tasks
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
        
        public async Task<IEnumerable<TaskObject>> GetActiveTasksItemsByUserIdAsync(int userId)
        {
            return await _context.Tasks
                .Include(t => t.User)
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }
        public async Task<TaskObject?> GetTaskByTaskIdUserIdAsync(int taskId, int userId)
        {
            return await _context.Tasks
                .Include(t => t.User)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
        }
        public async Task<TaskObject?> GetTaskByIdAsync(int id)
        {
            return await _context.Tasks
                .Include(t => t.User)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<TaskObject>> GetAllActiveTaskByStatus(int userId, string status)
        {
            return await _context.Tasks
                .Include(t => t.User)
                .Where(t => t.UserId == userId && t.Status.ToString() == status)
                .ToListAsync();
        }
        public async Task<TaskObject> UpdateTaskAsync(TaskObject task)
        {
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<bool> InactivateTaskAsync(TaskObject task)
        {
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReactivateTaskByIdAsync(int id)
        {
            var task = await _context.Tasks
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return false;
            task.IsActive = true;
            return await _context.SaveChangesAsync() > 0;
        }
        public async Task<bool> DeleteTaskAsync(TaskObject task)
        {
            _context.Tasks.Remove(task);
            await  _context.SaveChangesAsync();
            return true;
        }
    }
}
