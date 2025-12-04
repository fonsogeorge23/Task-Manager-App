using AutoMapper;
using Azure.Core;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
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
        Task<Result<TaskResponse>> ActivateTaskService(int taskId, int userIdFromToken);

        // Method to create task for an user
        Task<Result<TaskResponse>> CreateTaskService(TaskRequest request, int createId);
        Task<Result> DeleteTaskService(int id, int userIdFromToken);

        // Method to view the task for Admin and User
        Task<Result<TaskResponse>> GetTaskByIdService(int id, int userIdFromToken);

        // Method to get tasks for a user by status
        Task<Result<IEnumerable<TaskResponse>>> GetTaskForUserService(int userId, int userIdFromToken, string status);
        Task<Result<object>> GetTaskSummaryService(int userId, int userIdFromToken);
        Task<Result<TaskResponse>> InactivateTaskService(int taskId, int userIdFromToken);

        // Method to search tasks for a user by title or description
        Task<Result<IEnumerable<TaskResponse>>> SearchTasksService(int userId, int userIdFromToken, string query);
        Task<Result<TaskResponse>> UpdateTaskPriorityService(int taskId, string priority, int userIdFromToken);
        Task<Result<TaskResponse>> UpdateTaskService(int taskId, TaskRequest request, int userIdFromToken);
        Task<Result<TaskResponse>> UpdateTaskStatusService(int taskId, string status, int userIdFromToken);
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

        public Task<Result<TaskResponse>> ActivateTaskService(int taskId, int userIdFromToken)
        {
            throw new NotImplementedException();
        }

        // Method to create task for an user
        public async Task<Result<TaskResponse>> CreateTaskService(TaskRequest request, int createId)
        {
            // Authorized access - Admin can create task for any user
            var authorizedUser = await UserAuthentication(request.UserId, createId);
            if (!authorizedUser.IsSuccess)
            {
                return Result<TaskResponse>.Failure($"{authorizedUser.Message} - Failed to create new task");
            }

            // Validating the new task request
            var validRequest = await ValidateTaskRequest(request);
            if (!validRequest.IsSuccess)
            {
                return Result<TaskResponse>.Failure($"{validRequest.Message} - No new task created");
            }

            // returning the created task response
            return await AddNewTask(validRequest.Data!, createId);
        }

        public Task<Result> DeleteTaskService(int id, int userIdFromToken)
        {
            throw new NotImplementedException();
        }

        // Method to get a user task by Id
        public async Task<Result<TaskResponse>> GetTaskByIdService(int id, int userIdFromToken)
        {
            // Authorized access - Admin can view any task, User can view own task
            var userIdForTask = await GetUserFromTask(id);
            if (!userIdForTask.IsSuccess)
            {
                return Result<TaskResponse>.Failure(userIdForTask.Message!);
            }
            var authorizedUser = await UserAuthentication(userIdForTask.Data, userIdFromToken);
            if (!authorizedUser.IsSuccess)
            {
                return Result<TaskResponse>.Failure($"{authorizedUser.Message} - Cannot view the task");
            }

            // returnng the task response
            return await GetTaskById(id);
        }

        // Method to get tasks for a user by status
        public async Task<Result<IEnumerable<TaskResponse>>> GetTaskForUserService(int userId, int userIdFromToken, string status)
        {
            // Authorized access - Admin can view any user's tasks, User can view own tasks
            var authorizedUser = await UserAuthentication(userId, userIdFromToken);
            if (!authorizedUser.IsSuccess)
            {
                return Result<IEnumerable<TaskResponse>>.Failure($"{authorizedUser.Message} - Cannot view tasks for the user");
            }

            // Getting tasks for the user by status - default = "All"
            if (string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                var allTasks = await GetAllTaskForUser(userId);
                if (!allTasks.IsSuccess)
                {
                    return Result<IEnumerable<TaskResponse>>.Failure($"{allTasks.Message} - No task to load");
                }
                var responseAll = _mapper.Map<IEnumerable<TaskResponse>>(allTasks.Data);
                return Result<IEnumerable<TaskResponse>>.Success(responseAll, "All tasks retrieved successfully");
            }
            else
            {
                // Parsing the status for task
                var statusEnum = await ParseStatus(status);
                if (!statusEnum.IsSuccess)
                {
                    return Result<IEnumerable<TaskResponse>>.Failure($"{statusEnum.Message} - Failed to load tasks");
                }

                // Getting task by status
                var taskList = await GetAllTaskByStatus(userId, statusEnum.Data);
                if(taskList == null || !taskList.Any())
                {
                    return Result<IEnumerable<TaskResponse>>.Failure($"No tasks with status '{statusEnum.Data}' found for the user");
                }
                var responseAll = _mapper.Map<IEnumerable<TaskResponse>>(taskList);
                return Result<IEnumerable<TaskResponse>>.Success(responseAll, $"{statusEnum} - retrieved successfully");
            }
        }

        public Task<Result<object>> GetTaskSummaryService(int userId, int userIdFromToken)
        {
            throw new NotImplementedException();
        }

        public Task<Result<TaskResponse>> InactivateTaskService(int taskId, int userIdFromToken)
        {
            throw new NotImplementedException();
        }

        // Method to search tasks for a user by title or description
        public async Task<Result<IEnumerable<TaskResponse>>> SearchTasksService(int userId, int userIdFromToken, string query)
        {
            // Authorized access - Admin can search any user's tasks, User can search own tasks
            var authorizedUser = await UserAuthentication(userId, userIdFromToken);
            if (!authorizedUser.IsSuccess)
            {
                return Result<IEnumerable<TaskResponse>>.Failure($"{authorizedUser.Message} - Cannot search tasks for the user");
            }
            return Result<IEnumerable<TaskResponse>>.Success(Enumerable.Empty<TaskResponse>(), "No tasks found matching the query");
        }

        public Task<Result<TaskResponse>> UpdateTaskPriorityService(int taskId, string priority, int userIdFromToken)
        {
            throw new NotImplementedException();
        }

        public Task<Result<TaskResponse>> UpdateTaskService(int taskId, TaskRequest request, int userIdFromToken)
        {
            throw new NotImplementedException();
        }

        public Task<Result<TaskResponse>> UpdateTaskStatusService(int taskId, string status, int userIdFromToken)
        {
            throw new NotImplementedException();
        }



        /********************************************************
         *      private helper methods for public methods       *
         ********************************************************/

        // 01. UserAuthentication(userId)	        -> Method to authenticate user for an action
        // 02. ValidateTaskRequest(TaskRequest)     -> Method to validate the task request
        // 03. GetUser(userId)		                -> Method to get user info from userId
        // 04. GetTaskForUser(userId, title)  	    -> Method to get task with userId and title
        // 05. AddNewTask(validRequest, createId)	-> Method to add new task for a user
        // 06. GetUserFromTask(taskId)              -> Method to get userId from taskId
        // 07. GetTaskById(taskId)                  -> Method to get task by taskId
        // 08. GetAllTaskForUser                    -> Method to get all tasks for a user
        // 09. ParseStatus(status)                  -> Method to parse status string to enum
        // 10. GetAllTaskByStatus(userId, status)   -> Method to get all tasks for a user by status
        //******************************************************/

        // Helper method to authenticate user for an action
        private async Task<Result> UserAuthentication(int userId, int actorId)
        {
            var authorizedUser = await _userService.ValidateAccessUser(userId, actorId);
            if (!authorizedUser.IsSuccess)
            {
                return Result.Failure($"{authorizedUser.Message} - Authentication failed");
            }
            return Result.Success("Access authorized");
        }

        // Helper method to validate the task request
        private async Task<Result<TaskRequest>> ValidateTaskRequest(TaskRequest request)
        {
            if (request == null)
                return Result<TaskRequest>.Failure("Request cannot be null");

            var errors = new List<string>();

            // Mandatory fields
            if (string.IsNullOrWhiteSpace(request.Title))
                errors.Add("Task title is required.");
            if (!Enum.IsDefined(typeof(CurrentTaskStatus), request.Status))
                errors.Add("Invalid task status.");
            if (!Enum.IsDefined(typeof(PriorityLevel), request.PriorityLevel))
                errors.Add("Invalid priority level.");
            if (request.DueDate != default && request.DueDate < DateTime.UtcNow.Date)
                errors.Add("Due date cannot be in the past.");
            if (errors.Any())
                return Result<TaskRequest>.Failure(string.Join(" / ", errors));

            // Finding the assigned user
            int userId = request.UserId;
            var user = await GetUser(userId);
            if (!user.IsSuccess || !user.Data!.IsActive)
            {
                return Result<TaskRequest>.Failure($"{user.Message} - Invalid Task request");
            }

            // Checking duplicate task
            var title = request.Title;
            var exists = await GetTaskForUser(userId, title);
            if (exists.IsSuccess)
            {
                return Result<TaskRequest>.Failure($"{exists.Message} - Duplicate task");
            }
            return Result<TaskRequest>.Success(request);
        }

        // Helper method to get user info from userId
        private async Task<Result<User>> GetUser(int userId)
        {
            var user = await _userService.GetUserById(userId);
            if (!user.IsSuccess)
            {
                return Result<User>.Failure($"{user.Message} - User info not available");
            }
            return user;
        }

        // Helper method to get task with userId and title
        private async Task<Result<TaskObject>> GetTaskForUser(int userId, string title)
        {
            var user = await _taskRepository.SearchTaskForUser(userId, title);
            if (user == null)
            {
                return Result<TaskObject>.Failure("No task exist");
            }
            return Result<TaskObject>.Success(user, "Task exist");
        }

        // Helper method to add new task for a user
        private async Task<Result<TaskResponse>> AddNewTask(TaskRequest request, int createId)
        {
            var newTask = _mapper.Map<TaskObject>(request);
            var task = await _taskRepository.AddNewTaskAsync(newTask, createId);
            if (task == null)
            {
                return Result<TaskResponse>.Failure("Unable to create new task");
            }
            var response = _mapper.Map<TaskResponse>(task);
            return Result<TaskResponse>.Success(response, "Task created successfully");
        }

        // Helper method to get userId from taskId
        private async Task<Result<int>> GetUserFromTask(int taskId)
        {
            var task = await _taskRepository.GetTaskByIdAsync(taskId);
            if (task == null)
            {
                return Result<int>.Failure(-1, "No task found"); // Indicating task not found
            }
            if(task.AssignedToUser != null)
                return Result<int>.Success(task.AssignedToUserId!.Value);
            return Result<int>.Failure(-1, "No user for task");
        }

        // Helper method to get task by taskId
        private async Task<Result<TaskResponse>> GetTaskById(int taskId)
        {
            var task = await _taskRepository.GetTaskByIdAsync(taskId);
            if (task == null)
            {
                return Result<TaskResponse>.Failure("Task not found");
            }
            var response = _mapper.Map<TaskResponse>(task);
            return Result<TaskResponse>.Success(response, "Task retrieved successfully");
        }

        // Helper method to get all tasks for a user
        private async Task<Result<IEnumerable<TaskObject>>> GetAllTaskForUser(int userId)
        {
            var tasks = await _taskRepository.GetAllTasksForUserAsync(userId);
            if (tasks == null || !tasks.Any())
            {
                return Result<IEnumerable<TaskObject>>.Failure("No tasks found for the user");
            }
            return Result<IEnumerable<TaskObject>>.Success(tasks);
        }

        // Helper method to parse status string to enum
        private async Task<Result<CurrentTaskStatus>> ParseStatus(string status)
        {
            if (Enum.TryParse<CurrentTaskStatus>(status, true, out var statusEnum))
            {
                return Result<CurrentTaskStatus>.Success(statusEnum);
            }
            return Result<CurrentTaskStatus>.Failure("Invalid status value");
        }

        // Helper method to get all tasks for a user by status
        private async Task<IEnumerable<TaskObject>> GetAllTaskByStatus(int userId, CurrentTaskStatus statusEnum)
        {
            var allTasksResult = await GetAllTaskForUser(userId);
            if (!allTasksResult.IsSuccess)
            {
                return Enumerable.Empty<TaskObject>();
            }
            var filteredTasks = allTasksResult.Data!
                                      .Where(t => t.Status == statusEnum)
                                      .ToList();
            return filteredTasks;
        }
    }
}
