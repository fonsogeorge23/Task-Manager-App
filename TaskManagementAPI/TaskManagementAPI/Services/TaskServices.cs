using AutoMapper;
using System.Threading.Tasks;
using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.DTOs.Responses;
using TaskManagementAPI.Models;
using TaskManagementAPI.Repositories;
using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.Services
{
    public interface ITaskService
    {
        Task<Result<TaskResponse>> CreateTaskAsync(TaskRequest request);
        Task<Result<TaskResponse>> GetTaskByIdAsync(int id, int userId, string role);
        Task<IEnumerable<TaskResponse>> GetUserTasksAsync(int userId);
        Task<IEnumerable<TaskResponse>> GetAllTaskByStatusAsync(int userId, string status);
        Task<Result<TaskResponse>> UpdateTaskAsync(int id, TaskRequest request, int userId, string role);
        Task<Result<string>> SoftDeleteTask(int id, int userId);
        Task<Result<string>> DeleteTaskAsync(int id, int userId);
    }
    public class TaskServices : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;

        public TaskServices(ITaskRepository repository, IMapper mapper)
        {
            _taskRepository = repository;
            _mapper = mapper;
        }

        public async Task<Result<TaskResponse>> CreateTaskAsync(TaskRequest request)
        {
            // Map TaskRequest to TaskObject
            var task = _mapper.Map<TaskObject>(request);

            // Save to DB via repository
            var createdTask = await _taskRepository.AddTaskAsync(task);

            // Map TaskObject to TaskResponse and returning
            var response = _mapper.Map<TaskResponse>(createdTask);

            return Result<TaskResponse>.Success(response);
        }

        public async Task<Result<TaskResponse>> GetTaskByIdAsync(int id, int userId, string role)
        {
            // Retrieve task from repository
            var task = await _taskRepository.GetTaskByIdAsync(id);
            if (task == null)
            {
                return Result<TaskResponse>.Failure("Task not found");
            }

            if(task.UserId != userId && role != "Admin")
            {
                return Result<TaskResponse>.Failure("Access denied");
            }
            
            // Map to TaskResponse
            var response = _mapper.Map<TaskResponse>(task);

            return Result<TaskResponse>.Success(response);
        }
        public async Task<IEnumerable<TaskResponse>> GetUserTasksAsync(int userId)
        {
            var tasks = await _taskRepository.GetTasksItemsByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<TaskResponse>>(tasks);

        }
        public async Task<IEnumerable<TaskResponse>> GetAllTaskByStatusAsync(int userId, string status)
        {
            var tasks = await _taskRepository.GetAllTaskByStatus(userId, status);
            if(tasks == null || !tasks.Any())
            {
                return Enumerable.Empty<TaskResponse>();
            }
            return _mapper.Map<IEnumerable<TaskResponse>>(tasks);
        }
        public async Task<Result<TaskResponse>> UpdateTaskAsync(int taskId, TaskRequest request, int userId, string role)
        {
            var existingTask = await _taskRepository.GetTaskByTaskIdUserIdAsync(taskId, userId);
            if(existingTask == null)
            {
                if(role == "Admin")
                {
                    existingTask = await _taskRepository.GetTaskByIdAsync(taskId);
                }
                if(existingTask == null)
                {
                    return Result<TaskResponse>.Failure("Task not found or access denied");
                }
            }

            // Updating fields
            existingTask.Title = request.Title;
            existingTask.Description = request.Description;
            existingTask.Status = request.Status;
            existingTask.Priority = request.PriorityLevel;
            existingTask.DueDate = request.DueDate;
            existingTask.IsActive = request.IsActive;

            // Saving updated task
            var updatedTask = await _taskRepository.UpdateTaskAsync(existingTask);
            var response = _mapper.Map<TaskResponse>(updatedTask);
            return Result<TaskResponse>.Success(response);
        }

        public async Task<Result<string>> SoftDeleteTask(int id, int userId)
        {
            var tasks = await _taskRepository.GetTasksItemsByUserIdAsync(userId);
            if(tasks == null)
            {
                return Result<string>.Failure("No active task for user");
            }
            var updatingTask = tasks.FirstOrDefault(t => t.Id == id);
            if(updatingTask == null)
            {
                return Result<string>.Failure("Task not found");
            }
            await _taskRepository.SoftDeleteTask(updatingTask);
            return Result<string>.Success("Task is inactive");
        }

        public async Task<Result<string>> DeleteTaskAsync(int id, int userId)
        {
            var task = await _taskRepository.GetTasksItemsByUserIdAsync(userId);
            if (task == null)
            {
                return Result<string>.Failure("No active task for user");
            }
            var deletingTask = task.FirstOrDefault(t => t.Id == id);
            if (deletingTask == null)
            {
                return Result<string>.Failure("Task not found");
            }
            await _taskRepository.DeleteTaskAsync(deletingTask);
            return Result<string>.Success("Task deleted successfully");
        }
    }
}
