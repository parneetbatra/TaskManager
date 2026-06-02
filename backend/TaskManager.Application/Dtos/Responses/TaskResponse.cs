using TaskStatus = TaskManager.Domain.Enums.TaskStatus;

namespace TaskManager.Application.Dtos.Responses
{
    public sealed class TaskResponse
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = null!;
        public string? Description { get; init; }
        public TaskStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
