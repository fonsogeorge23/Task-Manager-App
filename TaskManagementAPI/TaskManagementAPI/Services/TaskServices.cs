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
        Task<Result<object>> ActivateTaskService(int taskId, int userIdFromToken);
        Task<Result<object>> CreateTaskService(TaskRequest request, int userIdFromToken);
        Task<Result<object>> DeleteTaskService(int id, int userIdFromToken);
        Task<Result<object>> GetTaskByIdService(int id, int userIdFromToken);
        Task<Result<IEnumerable<TaskResponse>>> GetTaskForUserService(int userId, int userIdFromToken, string v);
        Task<Result<object>> GetTaskSummaryService(int userId, int userIdFromToken);
        Task<Result<object>> InactivateTaskService(int taskId, int userIdFromToken);
        Task<Result<IEnumerable<TaskResponse>>> SearchTasksService(int userId, int userIdFromToken, string query);
        Task<Result<object>> UpdateTaskPriorityService(int taskId, string priority, int userIdFromToken);
        Task<Result<object>> UpdateTaskService(int taskId, TaskRequest request, int userIdFromToken);
        Task<Result<object>> UpdateTaskStatusService(int taskId, string status, int userIdFromToken);
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

        public Task<Result<object>> ActivateTaskService(int taskId, int userIdFromToken)
        {
            throw new NotImplementedException();
        }

        public Task<Result<object>> CreateTaskService(TaskRequest request, int userIdFromToken)
        {
            throw new NotImplementedException();
        }

        public Task<Result<object>> DeleteTaskService(int id, int userIdFromToken)
        {
            throw new NotImplementedException();
        }

        public Task<Result<object>> GetTaskByIdService(int id, int userIdFromToken)
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

        public Task<Result<object>> InactivateTaskService(int taskId, int userIdFromToken)
        {
            throw new NotImplementedException();
        }

        public Task<Result<IEnumerable<TaskResponse>>> SearchTasksService(int userId, int userIdFromToken, string query)
        {
            throw new NotImplementedException();
        }

        public Task<Result<object>> UpdateTaskPriorityService(int taskId, string priority, int userIdFromToken)
        {
            throw new NotImplementedException();
        }

        public Task<Result<object>> UpdateTaskService(int taskId, TaskRequest request, int userIdFromToken)
        {
            throw new NotImplementedException();
        }

        public Task<Result<object>> UpdateTaskStatusService(int taskId, string status, int userIdFromToken)
        {
            throw new NotImplementedException();
        }
    }
}
