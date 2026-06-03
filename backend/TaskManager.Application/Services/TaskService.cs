using Microsoft.Extensions.Logging;
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
        private readonly ILogger<TaskService> _logger;

        public TaskService(ITaskRepository repository, ILogger<TaskService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<PagedResult<TaskResponse>> GetTasksAsync(string statusFilter, int page, int pageSize)
        {
            _logger.LogInformation("Listing tasks with status filter '{StatusFilter}', page {Page}, size {PageSize}.", statusFilter, page, pageSize);

            var filter = (statusFilter ?? string.Empty).Trim();
            TaskStatus? status = null;

            if (!string.Equals(filter, "all", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(filter))
            {
                if (!Enum.TryParse<TaskStatus>(filter, ignoreCase: true, out var parsed))
                {
                    throw new ValidationException($"Unknown status filter '{filter}'. Allowed values are: all, {string.Join(", ", Enum.GetNames<TaskStatus>())}.");
                }

                status = parsed;
            }

            var pagedResult = await _repository.GetPagedAsync(status, page, pageSize);
            return new PagedResult<TaskResponse>
            {
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize,
                TotalCount = pagedResult.TotalCount,
                TotalPages = pagedResult.TotalPages,
                Items = pagedResult.Items.Select(ToResponse).ToList()
            };
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
            var title = request.Title.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ValidationException("Task title is required.");
            }

            var existingTasks = await _repository.GetAllAsync();
            if (existingTasks.Any(t => string.Equals(t.Title, title, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ValidationException("A task with the same title already exists.");
            }

            var now = DateTime.UtcNow;
            var task = new Domain.Entities.TaskItem
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Status = TaskStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            };

            var created = await _repository.CreateAsync(task);
            _logger.LogInformation("Created task {TaskId} with title '{Title}'.", created.Id, created.Title);
            return ToResponse(created);
        }

        public async Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing is null)
            {
                throw new EntityNotFoundException(id);
            }

            var title = request.Title.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ValidationException("Task title is required.");
            }

            var existingTasks = await _repository.GetAllAsync();
            if (existingTasks.Any(t => t.Id != id && string.Equals(t.Title, title, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ValidationException("A task with the same title already exists.");
            }

            if (existing.Status == TaskStatus.Cancelled && request.Status != TaskStatus.Cancelled)
            {
                throw new ValidationException("Cancelled tasks cannot be reopened or modified.");
            }

            existing.Title = title;
            existing.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            existing.Status = request.Status;
            existing.UpdatedAt = DateTime.UtcNow;

            var updated = await _repository.UpdateAsync(existing);
            if (!updated)
            {
                throw new EntityNotFoundException(id);
            }

            _logger.LogInformation("Updated task {TaskId} to status {Status}.", existing.Id, existing.Status);
            return ToResponse(existing);
        }

        public async Task DeleteAsync(Guid id)
        {
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                throw new EntityNotFoundException(id);
            }

            _logger.LogInformation("Deleted task {TaskId}.", id);
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
