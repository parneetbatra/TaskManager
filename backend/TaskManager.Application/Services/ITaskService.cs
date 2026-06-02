using TaskManager.Application.Dtos.Requests;
using TaskManager.Application.Dtos.Responses;

namespace TaskManager.Application.Services
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskResponse>> GetTasksAsync(string statusFilter);
        Task<TaskResponse> GetByIdAsync(Guid id);
        Task<TaskResponse> CreateAsync(CreateTaskRequest request);
        Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request);
        Task DeleteAsync(Guid id);
    }
}
