using TaskManager.Application.Dtos.Requests;
using TaskManager.Application.Dtos.Responses;

namespace TaskManager.Application.Services
{
    public interface ITaskService
    {
        Task<PagedResult<TaskResponse>> GetTasksAsync(string statusFilter, int page, int pageSize);
        Task<TaskResponse> GetByIdAsync(Guid id);
        Task<TaskResponse> CreateAsync(CreateTaskRequest request);
        Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request);
        Task DeleteAsync(Guid id);
    }
}
