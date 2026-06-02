using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Dtos.Requests;
using TaskManager.Application.Dtos.Responses;
using TaskManager.Application.Services;

namespace TaskManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        public async Task<IActionResult> Get([FromQuery] string status = "all")
        {
            var tasks = await _service.GetTasksAsync(status);
            return Ok(tasks);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var task = await _service.GetByIdAsync(id);
            return Ok(task);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var created = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var updated = await _service.UpdateAsync(id, request);
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
