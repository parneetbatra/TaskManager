using System.ComponentModel.DataAnnotations;

namespace TaskManager.Application.Dtos.Requests
{
    public sealed class CreateTaskRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }
    }
}
