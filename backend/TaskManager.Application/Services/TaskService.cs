using TaskManager.Application.Dtos.Requests;
using TaskManager.Application.Dtos.Responses;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Exceptions;
using TaskStatus = TaskManager.Domain.Enums.TaskStatus;

namespace TaskManager.Application.Services
{
    public sealed class TaskService : ITaskService
    {
        private readonly ITaskRepository _repository;

        public TaskService(ITaskRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<TaskResponse>> GetTasksAsync(string statusFilter)
        {
            var tasks = await _repository.GetAllAsync();
            var filter = (statusFilter ?? string.Empty).Trim();
            if (string.Equals(filter, "all", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(filter))
            {
                return tasks.Select(ToResponse);
            }

            if (Enum.TryParse<TaskStatus>(filter, ignoreCase: true, out var status))
            {
                return tasks.Where(t => t.Status == status).Select(ToResponse);
            }

            return tasks.Select(ToResponse);
        }

        public async Task<TaskResponse> GetByIdAsync(Guid id)
        {
            var task = await _repository.GetByIdAsync(id);
            if (task is null)
            {
                throw new EntityNotFoundException(id);
            }

            return ToResponse(task);
        }

        public async Task<TaskResponse> CreateAsync(CreateTaskRequest request)
        {
            var now = DateTime.UtcNow;
            var task = new Domain.Entities.TaskItem
            {
                Id = Guid.NewGuid(),
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Status = TaskStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            };

            var created = await _repository.CreateAsync(task);
            return ToResponse(created);
        }

        public async Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing is null)
            {
                throw new EntityNotFoundException(id);
            }

            existing.Title = request.Title.Trim();
            existing.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            existing.Status = request.Status;
            existing.UpdatedAt = DateTime.UtcNow;

            var updated = await _repository.UpdateAsync(existing);
            if (!updated)
            {
                throw new EntityNotFoundException(id);
            }

            return ToResponse(existing);
        }

        public async Task DeleteAsync(Guid id)
        {
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                throw new EntityNotFoundException(id);
            }
        }

        private static TaskResponse ToResponse(Domain.Entities.TaskItem entity)
            => new()
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
    }
}
