using AutoMapper;
using Azure.Core;
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
        Task<Result<TaskResponse>> GetTaskByIdService(int id, int userIdFromToken);
        Task<Result<IEnumerable<TaskResponse>>> GetTaskForUserService(int userId, int userIdFromToken, string v);
        Task<Result<object>> GetTaskSummaryService(int userId, int userIdFromToken);
        Task<Result<TaskResponse>> InactivateTaskService(int taskId, int userIdFromToken);
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
            return await AddNewTask(validRequest.Data!, createId);
        }

        public Task<Result> DeleteTaskService(int id, int userIdFromToken)
        {
            throw new NotImplementedException();
        }

        public Task<Result<TaskResponse>> GetTaskByIdService(int id, int userIdFromToken)
        {
            throw new NotImplementedException();
        }

        public Task<Result<IEnumerable<TaskResponse>>> GetTaskForUserService(int userId, int userIdFromToken, string v)
        {
            throw new NotImplementedException();
        }

        public Task<Result<object>> GetTaskSummaryService(int userId, int userIdFromToken)
        {
            throw new NotImplementedException();
        }

        public Task<Result<TaskResponse>> InactivateTaskService(int taskId, int userIdFromToken)
        {
            throw new NotImplementedException();
        }

        public Task<Result<IEnumerable<TaskResponse>>> SearchTasksService(int userId, int userIdFromToken, string query)
        {
            throw new NotImplementedException();
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
        //******************************************************/

        private async Task<Result> UserAuthentication(int userId, int actorId)
        {
            var user = await _userService.ValidateAccessUser(userId, actorId);
            if (!user.IsSuccess )
            {
                return Result.Failure($"{user.Message} - Authentication failed");
            }
            return Result.Success("Access authorized");
        }

        private async Task<Result<TaskRequest>> ValidateTaskRequest(TaskRequest request)
        {
            if (request == null)
                return Result<TaskRequest>.Failure("Request cannot be null");

            var errors = new List<string>();

            // Mandatory fields
            if(string.IsNullOrWhiteSpace(request.Title))
                errors.Add("Task title is required.");
            if (!Enum.IsDefined(typeof(CurrentStatus), request.Status))
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
            if(!user.IsSuccess || !user.Data!.IsActive)
            {
                return Result<TaskRequest>.Failure($"{user.Message} - Invalid Task request");
            }

            // Checking duplicate task
            var title = request.Title;
            var exists = await GetTaskForUser(userId, title);
            if(exists.IsSuccess)
            {
                return Result<TaskRequest>.Failure($"{exists.Message} - Duplicate task");
            }
            return Result<TaskRequest>.Success(request);
        }

        private async Task<Result<User>> GetUser(int userId)
        {
            var user = await _userService.GetUserById(userId);
            if (!user.IsSuccess)
            {
                return Result<User>.Failure($"{user.Message} - User info not available");
            }
            return user;
        }

        private async Task<Result<TaskObject>> GetTaskForUser(int userId, string title)
        {
            var user = await _taskRepository.SearchTaskForUser(userId, title);
            if(user == null)
            {
                return Result<TaskObject>.Failure("No task exist");
            }
            return Result<TaskObject>.Success(user, "Task exist");
        }

        private async Task<Result<TaskResponse>> AddNewTask(TaskRequest request, int createId)
        {
            var newTask = _mapper.Map<TaskObject>(request);
            var task = await _taskRepository.AddNewTaskAsync(newTask, createId);
            if(task == null)
            {
                return Result<TaskResponse>.Failure("Unable to create new task");
            }
            var response = _mapper.Map<TaskResponse>(task);
            return Result<TaskResponse>.Success(response, "Task created successfully");
        }
    }
}
