using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Data;
using TaskManagementAPI.Models;

namespace TaskManagementAPI.Repositories
{
    public interface ITaskRepository
    {
        Task<TaskObject> AddTaskAsync(TaskObject task);
        Task<IEnumerable<TaskObject>> GetTasksItemsByUserIdAsync(int userId);
        Task<IEnumerable<TaskObject>> GetAllTaskByStatus(int userId, string status);
        Task<TaskObject?> GetTaskByTaskIdUserIdAsync(int id, int userId);
        Task<TaskObject?> GetTaskByIdAsync(int id);
        Task<TaskObject> UpdateTaskAsync(TaskObject task);
        Task<bool> DeleteTaskAsync(TaskObject task);
        Task<bool> SoftDeleteTask(TaskObject task);

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

        public async Task<TaskObject?> GetTaskByTaskIdUserIdAsync(int id, int userId)
        {
            return await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        }

        public async Task<TaskObject?> GetTaskByIdAsync(int id)
        {
            return await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id);
        }
        public async Task<IEnumerable<TaskObject>> GetTasksItemsByUserIdAsync(int userId)
        {
            return await _context.Tasks
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskObject>> GetAllTaskByStatus(int userId, string status)
        {
            return await _context.Tasks
                .Where(t => t.UserId == userId && t.Status.ToString() == status)
                .ToListAsync();
        }
        public async Task<TaskObject> UpdateTaskAsync(TaskObject task)
        {
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<bool> SoftDeleteTask(TaskObject task)
        {
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteTaskAsync(TaskObject task)
        {
            _context.Tasks.Remove(task);
            await  _context.SaveChangesAsync();
            return true;
        }
    }
}
