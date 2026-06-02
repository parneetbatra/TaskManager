using TaskStatus = TaskManager.Domain.Enums.TaskStatus;

namespace TaskManager.Domain.Entities
{
    public sealed class TaskItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
