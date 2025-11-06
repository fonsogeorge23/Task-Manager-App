using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Data;
using TaskManagementAPI.Models;

namespace TaskManagementAPI.Repositories
{
    public interface ITaskRepository
    {
        Task<TaskObject> AddTaskAsync(TaskObject task);
        Task<IEnumerable<TaskObject>> GetAllTaskForUserAsync(int userId);
        Task<IEnumerable<TaskObject>> GetAllActiveTaskForUserAsync(int userId);
        Task<IEnumerable<TaskObject>> GetAllActiveTaskByStatus(int userId, string status);
        Task<TaskObject?> GetTaskByTaskIdUserIdAsync(int taskId, int userId);
        Task<TaskObject?> GetActiveTaskByTaskIdUserIdAsync(int id, int userId);
        Task<TaskObject?> GetActiveTaskByIdAsync(int taskId);
        Task<TaskObject?> GetTaskByIdAsync(int taskid);
        Task<TaskObject> UpdateTaskAsync(TaskObject task);
        Task<bool> DeleteTaskAsync(TaskObject task);

    }

    public class TaskRepository : ITaskRepository
    {
        private readonly AppDbContext _context;
        public TaskRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TaskObject> AddTaskAsync(TaskObject task)
        {
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<IEnumerable<TaskObject>> GetAllTaskForUserAsync(int userId)
        {
            return await _context.Tasks
                .Include(t => t.User)
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskObject>> GetAllActiveTaskForUserAsync(int userId)
        {
            return await _context.Tasks
                .Include(t => t.User)
                .Where(t => t.UserId == userId && t.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskObject>> GetAllActiveTaskByStatus(int userId, string status)
        {
            return await _context.Tasks
                .Include(t => t.User)
                .Where(t => t.UserId == userId
                        && t.Status.ToString() == status
                        && t.IsActive)
                .ToListAsync();
        }

        public async Task<TaskObject?> GetTaskByTaskIdUserIdAsync(int taskId, int userId)
        {
            return await _context.Tasks
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == taskId
                                            && t.UserId == userId);
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
                .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);
        }

        public async Task<TaskObject?> GetTaskByIdAsync(int taskId)
        {
            return await _context.Tasks
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }
        public async Task<TaskObject> UpdateTaskAsync(TaskObject task)
        {
            await _context.SaveChangesAsync();
            var newData = await GetTaskByIdAsync(task.Id);
            return newData!;
        }

        public async Task<bool> DeleteTaskAsync(TaskObject task)
        {
            //_context.Tasks.Remove(task);
            //await _context.SaveChangesAsync();
            return true;
        }

    }
}
