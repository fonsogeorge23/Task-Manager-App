using AutoMapper;
using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.DTOs.Responses;
using TaskManagementAPI.Models;
using TaskManagementAPI.Repositories;

namespace TaskManagementAPI.Services
{
    public interface ITaskService
    {
        Task<TaskResponse> CreateTaskAsync(TaskRequest request);
        //Task<bool> DeleteTaskAsync(int id, int userId);
        Task<TaskObject?> GetTaskByIdAsync(int id, int userId);
        //Task<IEnumerable<TaskObject>> GetUserTasksAsync(int userId);
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

        public async Task<TaskResponse> CreateTaskAsync(TaskRequest request)
        {
            // Map TaskRequest to TaskObject
            var task = _mapper.Map<TaskObject>(request);

            // Save to DB via repository
            var createdTask = await _taskRepository.AddTaskAsync(task);

            // Map TaskObject to TaskResponse and returning
            return _mapper.Map<TaskResponse>(createdTask);
        }

        //public Task<bool> DeleteTaskAsync(int id, int userId) => _taskRepository.DeleteTaskAsync(id, userId);

        public Task<TaskObject?> GetTaskByIdAsync(int id, int userId) => _taskRepository.GetTaskByIdAsync(id, userId);

        //public Task<IEnumerable<TaskObject>> GetUserTasksAsync(int userId) => _taskRepository.GetTasksItemsByUserIdAsync(userId);

        //public Task<TaskObject> UpdateTaskAsync(TaskObject task) => _taskRepository.UpdateTaskAsync(task);
    }
}
