using System.ComponentModel.DataAnnotations;
using TaskStatus = TaskManager.Domain.Enums.TaskStatus;

namespace TaskManager.Application.Dtos.Requests
{
    public sealed class UpdateTaskRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public TaskStatus Status { get; set; }
    }
}
