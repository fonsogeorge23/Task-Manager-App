using AutoMapper;
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
        Task<Result<TaskResponse>> GetTaskByIdAsync(int id, int userId);
        Task<IEnumerable<TaskResponse>> GetUserTasksAsync(int userId);
        //Task<bool> DeleteTaskAsync(int id, int userId);
        //Task<TaskObject?> UpdateTaskAsync(TaskObject task);
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

        public async Task<Result<TaskResponse>> GetTaskByIdAsync(int id, int userId)
        {
            // Retrieve task from repository
            var task = await _taskRepository.GetTaskByIdAsync(id, userId);
            if (task == null)
            {
                return Result<TaskResponse>.Failure("Task not found");
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

        //public Task<bool> DeleteTaskAsync(int id, int userId) => _taskRepository.DeleteTaskAsync(id, userId);



        //public Task<TaskObject> UpdateTaskAsync(TaskObject task) => _taskRepository.UpdateTaskAsync(task);
    }
}
