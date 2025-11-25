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

        public async Task<TaskObject> AddNewTaskAsync(TaskObject task, int createId)
        {
            _context.OverrideUserId = createId;

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Reset to avoid leaking state
            _context.OverrideUserId = null;
            return task;
        }

        public async Task<TaskObject?> SearchTaskForUser(int userId, string title)
        {
            return await _context.Tasks.FirstOrDefaultAsync(t => t.Title == title && 
                                                    t.UserId == userId);
        }
    }
}
