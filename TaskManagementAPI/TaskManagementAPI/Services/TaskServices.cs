using AutoMapper;
using Azure.Core;
using System.Data;
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
        // Method to create a new task
        Task<Result<TaskResponse>> CreateTaskAsync(TaskRequest request, int creatingId);

        // Method to get user task (active/inactive) with status 
        Task<Result<IEnumerable<TaskResponse>>> GetTasksForUserAsync(int id, int accessId, string status);
        
        // Method to get task by Id for a user
        Task<Result<TaskResponse>> GetActiveTaskByIdAsync(int taskId, int accessId);

        // Method to update the task data
        Task<Result<TaskResponse>> UpdateTaskAsync(int id, TaskRequest request, int accessId);

        // Method to Activate the inactive task
        Task<Result<TaskResponse>> ActivateTaskAsync(int taskId, int userId);

        // Method to inactivate task
        Task<Result<TaskResponse>> InactivateTask(int taskId, int accessId);

        // Method to delete task for a user
        Task<Result<string>> DeleteTaskAsync(int id, int userId);
    }
    public class TaskServices : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public TaskServices(ITaskRepository repository, IUserService userService, IMapper mapper)
        {
            _taskRepository = repository;
            _userService = userService;
            _mapper = mapper;
        }

        public async Task<Result<TaskResponse>> CreateTaskAsync(TaskRequest request, int creatingId)
        {
            // Access control
            var authorizedUser = await Authorized(creatingId, request.UserId);
            if (!authorizedUser.IsSuccess)
                return Result<TaskResponse>.Failure($"{authorizedUser.Message} to create task");

            // Map TaskRequest to TaskObject
            var task = _mapper.Map<TaskObject>(request);
            
            // Save to DB via repository
            var createdTask = await _taskRepository.AddTaskAsync(task);

            // Map TaskObject to TaskResponse and returning
            var response = _mapper.Map<TaskResponse>(createdTask);

            return Result<TaskResponse>.Success(response);
        }

        // Helper method to authorize the accessing and target user
        private async Task<Result> Authorized(int accessId, int userId)
        {
            // user active check
            bool activeAccessUser = await ActiveUserExist(accessId);
            bool activeTargetUser = await ActiveUserExist(userId);

            if (!activeAccessUser || !activeTargetUser)
                return Result.Failure("Access denied - inactive/no user");

            // authorization check
            var user = await AuthorizedActionCheck(accessId, userId);
            if (!user.IsSuccess)
                return Result.Failure(user.Message);
            return Result.Success();
        }

        // Helper method to check if user is active
        private async Task<bool> ActiveUserExist(int userId)
        {
            var user = await _userService.GetActiveUserById(userId);
            return user.IsSuccess;
        }

        // Helper method to check if action is authorized
        private async Task<Result> AuthorizedActionCheck(int accessId, int targetId)
        {
            var authorizationResult = await _userService.AuthorizedAction(accessId, targetId);
            if (!authorizationResult.IsSuccess)
                return Result.Failure(authorizationResult.Message);
            return Result.Success();
        }

        public async Task<Result<IEnumerable<TaskResponse>>> GetTasksForUserAsync(int userId, int accessId, string status)
        {
            // Access control
            var authorizedUser = await Authorized(accessId, userId);
            if (!authorizedUser.IsSuccess)
                return Result<IEnumerable<TaskResponse>>.Failure($"{authorizedUser.Message} to view tasks");

            // Get tasks for user
            IEnumerable<TaskResponse> tasks;
            if(status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                // Get all tasks
                tasks = await GetAllUserTasksAsync(userId);
            }
            else
            {
                // Get all tasks by status
                tasks = await GetAllActiveTaskByStatusAsync(userId, status);
            }

            if (tasks == null || !tasks.Any())
                return Result<IEnumerable<TaskResponse>>.Failure($"No {status ?? "active"} tasks for user");

            return Result<IEnumerable<TaskResponse>>.Success(tasks);
        }

        // Helper method to get all(active and inactive) tasks for a user
        private async Task<IEnumerable<TaskResponse>> GetAllUserTasksAsync(int userId)
        {
            var tasks = await _taskRepository.GetAllTaskForUserAsync(userId);
            var response = _mapper.Map<IEnumerable<TaskResponse>>(tasks);

            return response?? Enumerable.Empty<TaskResponse>();
        }

        // Helper method to get active tasks by status for a user
        private async Task<IEnumerable<TaskResponse>> GetAllActiveTaskByStatusAsync(int userId, string status)
        {
            IEnumerable<TaskObject> tasks;

            if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                tasks = await GetAllActiveTaskByUserId(userId);
            }
            else
            {
                tasks = await GetAllActiveTaskByStatus(userId, status);
            }

            var response = _mapper.Map<IEnumerable<TaskResponse>>(tasks);

            return response ?? Enumerable.Empty<TaskResponse>();
        }

        // Helper method to get all active task by userId
        private async Task<IEnumerable<TaskObject>> GetAllActiveTaskByUserId(int userId)
        {
            var allActiveTasks = await _taskRepository.GetAllActiveTaskForUserAsync(userId);
            if (allActiveTasks == null || !allActiveTasks.Any())
            {
                return Enumerable.Empty<TaskObject>();
            }
            var response = _mapper.Map<IEnumerable<TaskObject>>(allActiveTasks);
            return response;
        }

        // Helper method to get active task with status
        private async Task<IEnumerable<TaskObject>> GetAllActiveTaskByStatus(int userId, string status)
        {
            return await _taskRepository.GetAllActiveTaskByStatus(userId, status);
        }

        /*
         NEED TO WORK ON THIS
         */
        public async Task<Result<TaskResponse>> GetActiveTaskByIdAsync(int taskId, int accessId)
        {
            // Retrieve task from repository
            var task = await GetActiveTaskById(taskId);
            if (task == null)
            {
                return Result<TaskResponse>.Failure("Task not found / Inactive");
            }

            // Access control
            var authorizedUser = await Authorized(accessId, task.UserId);
            if (!authorizedUser.IsSuccess)
                return Result<TaskResponse>.Failure($"{authorizedUser.Message} to view task");

            // Map to TaskResponse
            var response = _mapper.Map<TaskResponse>(task);
            return Result<TaskResponse>.Success(response);
        }

        // Helper method to get the active task by Id
        private async Task<TaskObject?> GetActiveTaskById(int taskId)
        {
            return await _taskRepository.GetActiveTaskByIdAsync(taskId);
        }

        public async Task<Result<TaskResponse>> UpdateTaskAsync(int taskId, TaskRequest request, int accessId)
        {
            var task = await GetTaskByTaskIdUserId(taskId, request.UserId);
            if (task.IsSuccess)
            {
                return Result<TaskResponse>.Failure($"{task.Message} - Failed to update the task");
            }

            var updatedTask = await UpdateTaskAsync(task.Data!);
            if (!updatedTask.IsSuccess)
                return Result<TaskResponse>.Failure("Failed to update task");

            var response = _mapper.Map<TaskResponse>(updatedTask);
            return Result<TaskResponse>.Success(response);
        }

        // Helper method to update the task in DB
        private async Task<Result<TaskObject>> UpdateTaskAsync(TaskObject task)
        {
            var updatingTask = await _taskRepository.UpdateTaskAsync(task);
            return Result<TaskObject>.Success(updatingTask);
        }

        public async Task<Result<TaskResponse>> ActivateTaskAsync(int taskId, int accessId)
        {
            var task = await GetTaskByTaskIdUserId(taskId, accessId);
            if (!task.IsSuccess)
            {
                return Result<TaskResponse>.Failure($"{task.Message} - Failed to activate");
            }

            if (task.Data!.IsActive)
            {
                var response = _mapper.Map<TaskResponse>(task.Data); 
                return Result<TaskResponse>.Success(response, "Task already active");
            }

            // If not active, activate and update
            task.Data.IsActive = true;
            var activatedTask = await UpdateTaskAsync(task.Data);
            var activatedResponse = _mapper.Map<TaskResponse>(activatedTask.Data); 
            return Result<TaskResponse>.Success(activatedResponse, "Task activated");
        }

        // Helper method to get any task with taskId and accessId
        private async Task<Result<TaskObject>> GetTaskByTaskIdUserId(int taskId, int accessId)
        {
            var task = await GetTaskObjectByIdAsync(taskId);
            if(task == null)
            {
                return Result<TaskObject>.Failure("No task found");
            }

            // Access Control 
            var authorizedUser = await Authorized(accessId, task.UserId);
            if (!authorizedUser.IsSuccess)
                return Result<TaskObject>.Failure($"{authorizedUser.Message} - Unauthorized");

            return Result<TaskObject>.Success(task);
        }
        
        // Helper method to get task (active/inactive) by taskid
        private async Task<TaskObject?> GetTaskObjectByIdAsync(int taskId)
        {
            var task = await _taskRepository.GetTaskByIdAsync(taskId); 
            return task == null ? null : task;
        }


        public async Task<Result<TaskResponse>> InactivateTask(int taskId, int accessId)
        {
            var task = await GetActiveTaskByTaskIdUserId(taskId, accessId);
            if (!task.IsSuccess)
            {
                return Result<TaskResponse>.Failure($"{task.Message} - Failed to inactivate the task");
            }
            task.Data!.IsActive = false;
            var updatedTask = await UpdateTaskAsync(task.Data);
            var response = _mapper.Map<TaskResponse>(updatedTask.Data);
            return Result<TaskResponse>.Success(response);
        }

        // Helper method to get task for a user
        private async Task<Result<TaskObject>> GetActiveTaskByTaskIdUserId(int taskId, int accessId)
        {
            var task = await GetActiveTaskById(taskId);
            if (task == null)
            {
                return Result<TaskObject>.Failure("No task found / Inactive");
            }
            // Access control
            var authorizedUser = await Authorized(accessId, task.UserId);
            if (!authorizedUser.IsSuccess)
                return Result<TaskObject>.Failure("Unauthorized");

            var userTask = await _taskRepository.GetActiveTaskByTaskIdUserIdAsync(taskId, task.UserId);
            return Result<TaskObject>.Success(userTask!);
        }

        public async Task<Result<string>> DeleteTaskAsync(int id, int accessId)
        {
            var deletingTask = await GetTaskByTaskIdUserId(id, accessId);
            if (!deletingTask.IsSuccess)
            {
                return Result<string>.Failure($"{deletingTask.Message} to delete task");
            }
            await _taskRepository.DeleteTaskAsync(deletingTask.Data!);
            return Result<string>.Success("Task deleted successfully");
        }
    }
}
