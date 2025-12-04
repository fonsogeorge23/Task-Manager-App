using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Data;
using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.Models;

namespace TaskManagementAPI.Repositories
{
    public interface ITaskRepository
    {
        // Method to add new task for a user
        Task<TaskObject> AddNewTaskAsync(TaskObject task, int createId);

        // Method to get all tasks for a user
        Task<IEnumerable<TaskObject>> GetAllTasksForUserAsync(int userId);

        // Method to get task by id
        Task<TaskObject?> GetTaskByIdAsync(int taskId);

        // Method to search task for user with title
        Task<TaskObject?>SearchTaskForUser(int userId, string title);
    }

    public class TaskRepository : ITaskRepository
    {
        private readonly AppDbContext _context;
        public TaskRepository(AppDbContext context)
        {
            _context = context;
        }

        // Repo method to add new task for a user
        public async Task<TaskObject> AddNewTaskAsync(TaskObject task, int createId)
        {
            _context.OverrideUserId = createId;

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Reset to avoid leaking state
            _context.OverrideUserId = null;
            return task;
        }

        // Repo method to get all tasks for a user
        public async Task<IEnumerable<TaskObject>> GetAllTasksForUserAsync(int userId)
        {
            return await _context.Tasks
                                 .Where(t => t.AssignedToUserId == userId)
                                 .ToListAsync();
        }

        // Repo method to get task by id
        public async Task<TaskObject?> GetTaskByIdAsync(int taskId)
        {
            return await _context.Tasks.FindAsync(taskId);
        }

        // Repo method to search task for user with title
        public async Task<TaskObject?> SearchTaskForUser(int userId, string title)
        {
            return await _context.Tasks.FirstOrDefaultAsync(t => t.Title == title && 
                                                    t.AssignedToUserId == userId);
        }
    }
}
