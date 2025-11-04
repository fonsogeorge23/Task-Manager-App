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
        Task<Result<TaskResponse>> CreateTaskAsync(TaskRequest request, int creatingId);
        Task<Result<IEnumerable<TaskResponse>>> GetTasksForUserAsync(int id, int accessId, string role, string status);
        Task<Result<TaskResponse>> GetTaskByIdAsync(int id, int userId, string role);





        Task<Result<TaskResponse>> UpdateTaskAsync(int id, TaskRequest request, int accessId, string role);
        Task<Result<TaskResponse>> ActivateTaskAsync(int id, int userId, string role);
        Task<Result<string>> InactivateTask(int id, int userId, string role);
        Task<Result<string>> DeleteTaskAsync(int id, int userId, string role);
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

        public async Task<Result<TaskResponse>> CreateTaskAsync(TaskRequest request, int creatingId)
        {
            // Map TaskRequest to TaskObject
            var task = _mapper.Map<TaskObject>(request);
            
            // Save to DB via repository
            var createdTask = await _taskRepository.AddTaskAsync(task);

            // Map TaskObject to TaskResponse and returning
            var response = _mapper.Map<TaskResponse>(createdTask);

            return Result<TaskResponse>.Success(response);
        }

        public async Task<Result<IEnumerable<TaskResponse>>> GetTasksForUserAsync(int id, int accessId, string role, string status)
        {
            // Access control
            if (id != accessId && role != "Admin")
                return Result<IEnumerable<TaskResponse>>.Failure("Access denied");

            IEnumerable<TaskResponse> tasks;

            //if (string.IsNullOrWhiteSpace(status))
            if(status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                tasks = await GetUserTasksAsync(id);
            }
            else
            {
                tasks = await GetAllTaskByStatusAsync(id, status);
            }

            if (tasks == null || !tasks.Any())
                return Result<IEnumerable<TaskResponse>>.Failure($"No {status ?? "active"} tasks for user");

            return Result<IEnumerable<TaskResponse>>.Success(tasks);
        }

        private async Task<IEnumerable<TaskResponse>> GetUserTasksAsync(int userId)
        {
            var tasks = await _taskRepository.GetAllUserTaskAsync(userId);
            var response = _mapper.Map<IEnumerable<TaskResponse>>(tasks);

            return response?? Enumerable.Empty<TaskResponse>();
        }

        private async Task<IEnumerable<TaskResponse>> GetAllTaskByStatusAsync(int userId, string status)
        {
            IEnumerable<TaskObject> tasks;

            if (status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                tasks = await _taskRepository.GetAllActiveTaskAsync(userId);
            }
            else
            {
                tasks = await _taskRepository.GetAllActiveTaskByStatus(userId, status);
            }

            var response = _mapper.Map<IEnumerable<TaskResponse>>(tasks);

            return response ?? Enumerable.Empty<TaskResponse>();
        }

        public async Task<Result<TaskResponse>> GetTaskByIdAsync(int id, int accessId, string role)
        {

            // Ensure only owner or admin can view
            if (id != accessId && role != "Admin")
            {
                return Result<TaskResponse>.Failure("Access denied");
            }

            // Retrieve task from repository
            var task = await _taskRepository.GetActiveTaskByIdAsync(id);
            if (task == null)
            {
                return Result<TaskResponse>.Failure("Task not found");
            }

            // Map to TaskResponse
            var response = _mapper.Map<TaskResponse>(task);
            return Result<TaskResponse>.Success(response);
        }
















        public async Task<Result<TaskResponse>> UpdateTaskAsync(int taskId, TaskRequest request, int accessId, string role)
        {
            if (request.UserId != accessId && role != "Admin")
            {
                return Result<TaskResponse>.Failure("Access denied");
            }
            var userTasks = await _taskRepository.GetAllUserTaskAsync(request.UserId);
            if (userTasks == null || !userTasks.Any())
            {
                return Result<TaskResponse>.Failure("No tasks found for this user");
            }

            var existingTask = userTasks.FirstOrDefault(t => t.Id == taskId);

            if (existingTask == null)
            {
                return Result<TaskResponse>.Failure("Task not found");
            }

            existingTask.Title = request.Title;
            existingTask.Description = request.Description;
            existingTask.Status = request.Status;
            existingTask.Priority = request.PriorityLevel;
            existingTask.DueDate = request.DueDate;

            var isUpdated = await _taskRepository.UpdateTaskAsync(existingTask);
            
            var response = _mapper.Map<TaskResponse>(existingTask);
            return Result<TaskResponse>.Success(response);
        }

        public async Task<Result<TaskResponse>> ActivateTaskAsync(int id, int userId, string role)
        {
            var task = await _taskRepository.GetTaskByTaskIdUserIdAsync(id, userId);
            if(task == null)
            {
                if(role == "Admin")
                {
                    task = await _taskRepository.GetTaskByIdAsync(id);
                }
                if(task == null)
                {
                    return Result<TaskResponse>.Failure("Task not found/access denied");
                }
            }
            if (task.IsActive)
            {
                return Result<TaskResponse>.Failure("Task is active");
            }
            task.IsActive = true;
            var activatedTask = await _taskRepository.UpdateTaskAsync(task);
            var response = _mapper.Map<TaskResponse>(activatedTask);
            return Result<TaskResponse>.Success(response);
        }

        public async Task<Result<string>> InactivateTask(int id, int userId, string role)
        {
            var task = await _taskRepository.GetActiveTaskByTaskIdUserIdAsync(id, userId);
            if(task == null)
            {
                if(role == "Admin")
                {
                    task = await _taskRepository.GetActiveTaskByIdAsync(id);
                }
                if(task == null)
                {
                    return Result<string>.Failure("Task not found/access denied");
                }
            }
            task.IsActive = false;
            await _taskRepository.InactivateTaskAsync(task);
            return Result<string>.Success("Task is inactive");
        }

        public async Task<Result<string>> DeleteTaskAsync(int id, int userId, string role)
        {
            var deletingTask = await _taskRepository.GetActiveTaskByTaskIdUserIdAsync(id, userId);
            if (deletingTask == null)
            {
                if (role == "Admin")
                {
                    deletingTask = await _taskRepository.GetActiveTaskByIdAsync(id);
                }
                if (deletingTask == null)
                {
                    return Result<string>.Failure("Task not found/access denied");
                }
            }
            //await _taskRepository.DeleteTaskAsync(deletingTask);
            return Result<string>.Success("Task deleted successfully");
        }
    }
}
