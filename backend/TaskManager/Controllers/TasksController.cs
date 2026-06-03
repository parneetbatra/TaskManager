using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Dtos.Requests;
using TaskManager.Application.Dtos.Responses;
using TaskManager.Application.Services;

namespace TaskManager.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public sealed class TasksController : ControllerBase
    {
        private readonly ITaskService _service;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ITaskService service, ILogger<TasksController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] string status = "all",
            [FromQuery, Range(1, int.MaxValue)] int page = 1,
            [FromQuery, Range(1, 100)] int pageSize = 10)
        {
            _logger.LogInformation("Listing tasks with filter '{StatusFilter}', page {Page}, page size {PageSize}.", status, page, pageSize);
            var tasks = await _service.GetTasksAsync(status, page, pageSize);
            return Ok(tasks);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            _logger.LogInformation("Retrieving task {TaskId}.", id);
            var task = await _service.GetByIdAsync(id);
            return Ok(task);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create task request failed validation.");
                return ValidationProblem(ModelState);
            }

            var created = await _service.CreateAsync(request);
            _logger.LogInformation("Created task {TaskId}.", created.Id);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Update task {TaskId} request failed validation.", id);
                return ValidationProblem(ModelState);
            }

            var updated = await _service.UpdateAsync(id, request);
            _logger.LogInformation("Updated task {TaskId}.", id);
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            _logger.LogInformation("Deleted task {TaskId}.", id);
            return NoContent();
        }
    }
}
