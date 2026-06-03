using Microsoft.Extensions.Logging.Abstractions;
using TaskManager.Application.Dtos.Requests;
using TaskManager.Application.Dtos.Responses;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Exceptions;
using TaskStatus = TaskManager.Domain.Enums.TaskStatus;

namespace TaskManager.Tests
{
    public class TaskServiceTests
    {
        [Fact]
        public async Task GetTasksAsync_ReturnsAllTasks_WhenFilterIsAll()
        {
            var taskA = CreateTask(title: "Task A", status: TaskStatus.Active);
            var taskB = CreateTask(title: "Task B", status: TaskStatus.Completed);
            var service = CreateService(taskA, taskB);

            var result = (await service.GetTasksAsync("all", 1, 20)).Items.ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, task => task.Title == "Task A");
            Assert.Contains(result, task => task.Title == "Task B");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetTasksAsync_ReturnsAllTasks_WhenFilterIsBlank(string filter)
        {
            var service = CreateService(
                CreateTask(title: "Task A", status: TaskStatus.Active),
                CreateTask(title: "Task B", status: TaskStatus.Completed));

            var result = (await service.GetTasksAsync(filter, 1, 20)).Items.ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetTasksAsync_FiltersByStatus()
        {
            var service = CreateService(
                CreateTask(title: "Active Task", status: TaskStatus.Active),
                CreateTask(title: "Completed Task", status: TaskStatus.Completed));

            var result = (await service.GetTasksAsync("completed", 1, 20)).Items.ToList();

            var item = Assert.Single(result);
            Assert.Equal("Completed Task", item.Title);
            Assert.Equal(TaskStatus.Completed, item.Status);
        }

        [Fact]
        public async Task GetTasksAsync_ThrowsValidationException_WhenFilterIsUnknown()
        {
            var service = CreateService();

            var action = () => service.GetTasksAsync("invalid-status", 1, 20);

            var exception = await Assert.ThrowsAsync<ValidationException>(action);
            Assert.Contains("Unknown status filter", exception.Message);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsTask_WhenItExists()
        {
            var task = CreateTask(title: "Existing Task");
            var service = CreateService(task);

            var result = await service.GetByIdAsync(task.Id);

            Assert.Equal(task.Id, result.Id);
            Assert.Equal("Existing Task", result.Title);
        }

        [Fact]
        public async Task GetByIdAsync_ThrowsEntityNotFoundException_WhenTaskDoesNotExist()
        {
            var service = CreateService();

            var action = () => service.GetByIdAsync(Guid.NewGuid());

            await Assert.ThrowsAsync<EntityNotFoundException>(action);
        }

        [Fact]
        public async Task CreateAsync_TrimsValues_AndCreatesActiveTask()
        {
            var service = CreateService();

            var result = await service.CreateAsync(new CreateTaskRequest
            {
                Title = "  New Task  ",
                Description = "  Description  "
            });

            Assert.Equal("New Task", result.Title);
            Assert.Equal("Description", result.Description);
            Assert.Equal(TaskStatus.Active, result.Status);
            Assert.NotEqual(Guid.Empty, result.Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateAsync_ThrowsValidationException_WhenTitleIsBlank(string title)
        {
            var service = CreateService();

            var action = () => service.CreateAsync(new CreateTaskRequest { Title = title });

            var exception = await Assert.ThrowsAsync<ValidationException>(action);
            Assert.Equal("Task title is required.", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_ThrowsValidationException_WhenTitleAlreadyExists()
        {
            var service = CreateService(CreateTask(title: "Existing Task"));

            var action = () => service.CreateAsync(new CreateTaskRequest { Title = "existing task" });

            var exception = await Assert.ThrowsAsync<ValidationException>(action);
            Assert.Equal("A task with the same title already exists.", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesTask_WhenRequestIsValid()
        {
            var existing = CreateTask(title: "Old Title", description: "Old Description", status: TaskStatus.Active);
            var service = CreateService(existing);

            var result = await service.UpdateAsync(existing.Id, new UpdateTaskRequest
            {
                Title = "  Updated Title  ",
                Description = "  Updated Description  ",
                Status = TaskStatus.Completed
            });

            Assert.Equal(existing.Id, result.Id);
            Assert.Equal("Updated Title", result.Title);
            Assert.Equal("Updated Description", result.Description);
            Assert.Equal(TaskStatus.Completed, result.Status);
            Assert.True(result.UpdatedAt >= result.CreatedAt);
        }

        [Fact]
        public async Task UpdateAsync_ThrowsEntityNotFoundException_WhenTaskDoesNotExist()
        {
            var service = CreateService();

            var action = () => service.UpdateAsync(Guid.NewGuid(), new UpdateTaskRequest
            {
                Title = "Updated Title",
                Status = TaskStatus.Active
            });

            await Assert.ThrowsAsync<EntityNotFoundException>(action);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateAsync_ThrowsValidationException_WhenTitleIsBlank(string title)
        {
            var existing = CreateTask(title: "Existing Task");
            var service = CreateService(existing);

            var action = () => service.UpdateAsync(existing.Id, new UpdateTaskRequest
            {
                Title = title,
                Status = TaskStatus.Active
            });

            var exception = await Assert.ThrowsAsync<ValidationException>(action);
            Assert.Equal("Task title is required.", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_ThrowsValidationException_WhenAnotherTaskUsesSameTitle()
        {
            var target = CreateTask(title: "Target Task");
            var duplicate = CreateTask(title: "Existing Task");
            var service = CreateService(target, duplicate);

            var action = () => service.UpdateAsync(target.Id, new UpdateTaskRequest
            {
                Title = "existing task",
                Status = TaskStatus.Active
            });

            var exception = await Assert.ThrowsAsync<ValidationException>(action);
            Assert.Equal("A task with the same title already exists.", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_ThrowsValidationException_WhenCancelledTaskIsReopened()
        {
            var cancelled = CreateTask(title: "Cancelled Task", status: TaskStatus.Cancelled);
            var service = CreateService(cancelled);

            var action = () => service.UpdateAsync(cancelled.Id, new UpdateTaskRequest
            {
                Title = "Cancelled Task",
                Description = cancelled.Description,
                Status = TaskStatus.Active
            });

            var exception = await Assert.ThrowsAsync<ValidationException>(action);
            Assert.Equal("Cancelled tasks cannot be reopened or modified.", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_ThrowsEntityNotFoundException_WhenRepositoryUpdateReturnsFalse()
        {
            var existing = CreateTask(title: "Existing Task");
            var repository = new FakeTaskRepository(new[] { existing })
            {
                UpdateResult = false
            };
            var service = CreateService(repository);

            var action = () => service.UpdateAsync(existing.Id, new UpdateTaskRequest
            {
                Title = "Updated Title",
                Status = TaskStatus.Completed
            });

            await Assert.ThrowsAsync<EntityNotFoundException>(action);
        }

        [Fact]
        public async Task DeleteAsync_Completes_WhenRepositoryDeletesTask()
        {
            var existing = CreateTask(title: "Existing Task");
            var repository = new FakeTaskRepository(new[] { existing });
            var service = CreateService(repository);

            await service.DeleteAsync(existing.Id);

            Assert.DoesNotContain(repository.Tasks, task => task.Id == existing.Id);
        }

        [Fact]
        public async Task DeleteAsync_ThrowsEntityNotFoundException_WhenRepositoryReturnsFalse()
        {
            var repository = new FakeTaskRepository();
            var service = CreateService(repository);

            var action = () => service.DeleteAsync(Guid.NewGuid());

            await Assert.ThrowsAsync<EntityNotFoundException>(action);
        }

        private static TaskService CreateService(params TaskItem[] tasks)
            => CreateService(new FakeTaskRepository(tasks));

        private static TaskService CreateService(FakeTaskRepository repository)
            => new(repository, NullLogger<TaskService>.Instance);

        private static TaskItem CreateTask(
            string title,
            string? description = null,
            TaskStatus status = TaskStatus.Active)
        {
            var now = DateTime.UtcNow.AddMinutes(-5);

            return new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                Status = status,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        private sealed class FakeTaskRepository : ITaskRepository
        {
            public FakeTaskRepository(IEnumerable<TaskItem>? tasks = null)
            {
                Tasks = tasks?.Select(Clone).ToList() ?? new List<TaskItem>();
            }

            public List<TaskItem> Tasks { get; }

            public bool UpdateResult { get; set; } = true;

            public Task<IEnumerable<TaskItem>> GetAllAsync()
                => Task.FromResult<IEnumerable<TaskItem>>(Tasks.Select(Clone).ToList());

            public Task<PagedResult<TaskItem>> GetPagedAsync(TaskStatus? statusFilter, int page, int pageSize)
            {
                if (page < 1)
                {
                    page = 1;
                }

                var query = Tasks.AsQueryable();
                if (statusFilter.HasValue)
                {
                    query = query.Where(task => task.Status == statusFilter.Value);
                }

                var totalCount = query.Count();
                var items = query
                    .OrderByDescending(task => task.UpdatedAt)
                    .ThenByDescending(task => task.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(Clone)
                    .ToList();

                var totalPages = totalCount == 0
                    ? 1
                    : (int)Math.Ceiling(totalCount / (double)pageSize);

                return Task.FromResult(new PagedResult<TaskItem>
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    Items = items
                });
            }

            public Task<TaskItem?> GetByIdAsync(Guid id)
            {
                var task = Tasks.FirstOrDefault(t => t.Id == id);
                return Task.FromResult(task is null ? null : Clone(task));
            }

            public Task<TaskItem> CreateAsync(TaskItem task)
            {
                var created = Clone(task);
                Tasks.Add(created);
                return Task.FromResult(Clone(created));
            }

            public Task<bool> UpdateAsync(TaskItem task)
            {
                var index = Tasks.FindIndex(t => t.Id == task.Id);
                if (index < 0 || !UpdateResult)
                {
                    return Task.FromResult(false);
                }

                Tasks[index] = Clone(task);
                return Task.FromResult(true);
            }

            public Task<bool> DeleteAsync(Guid id)
            {
                var removed = Tasks.RemoveAll(t => t.Id == id) > 0;
                return Task.FromResult(removed);
            }

            private static TaskItem Clone(TaskItem task)
                => new()
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    Status = task.Status,
                    CreatedAt = task.CreatedAt,
                    UpdatedAt = task.UpdatedAt
                };
        }
    }
}