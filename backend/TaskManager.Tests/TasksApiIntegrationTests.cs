using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Dtos.Requests;
using TaskManager.Application.Dtos.Responses;
using TaskManager.Tests.Infrastructure;
using TaskStatus = TaskManager.Domain.Enums.TaskStatus;

namespace TaskManager.Tests
{
    public class TasksApiIntegrationTests
    {
        [Fact]
        public async Task CreateTask_ThenGetById_ReturnsCreatedTask()
        {
            using var factory = new TaskManagerWebApplicationFactory();
            using var client = factory.CreateClient();

            var createResponse = await client.PostAsJsonAsync("/api/v1/tasks", new CreateTaskRequest
            {
                Title = "Integration Task",
                Description = "Created through API"
            });

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();
            Assert.NotNull(createdTask);

            var getResponse = await client.GetAsync($"/api/v1/tasks/{createdTask!.Id}");

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var fetchedTask = await getResponse.Content.ReadFromJsonAsync<TaskResponse>();
            Assert.NotNull(fetchedTask);
            Assert.Equal(createdTask.Id, fetchedTask!.Id);
            Assert.Equal("Integration Task", fetchedTask.Title);
            Assert.Equal("Created through API", fetchedTask.Description);
            Assert.Equal(TaskStatus.Active, fetchedTask.Status);
        }

        [Fact]
        public async Task GetTasks_WithInvalidStatusFilter_ReturnsBadRequestProblemDetails()
        {
            using var factory = new TaskManagerWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/v1/tasks?status=invalid-status");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal((int)HttpStatusCode.BadRequest, problem!.Status);
            Assert.Contains("Unknown status filter", problem.Title);
        }

        [Fact]
        public async Task GetTask_WhenTaskDoesNotExist_ReturnsNotFoundProblemDetails()
        {
            using var factory = new TaskManagerWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync($"/api/v1/tasks/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal((int)HttpStatusCode.NotFound, problem!.Status);
            Assert.Contains("was not found", problem.Title);
        }

        [Fact]
        public async Task CreateTask_WithInvalidModel_ReturnsValidationProblemDetails()
        {
            using var factory = new TaskManagerWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/v1/tasks", new CreateTaskRequest
            {
                Title = string.Empty
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal((int)HttpStatusCode.BadRequest, problem!.Status);
            Assert.Contains("Title", problem.Errors.Keys);
        }

        [Fact]
        public async Task DeleteTask_RemovesTaskFromSubsequentReads()
        {
            using var factory = new TaskManagerWebApplicationFactory();
            using var client = factory.CreateClient();

            var createResponse = await client.PostAsJsonAsync("/api/v1/tasks", new CreateTaskRequest
            {
                Title = "Task To Delete",
                Description = "Delete me"
            });
            var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

            Assert.NotNull(createdTask);

            var deleteResponse = await client.DeleteAsync($"/api/v1/tasks/{createdTask!.Id}");

            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            var getResponse = await client.GetAsync($"/api/v1/tasks/{createdTask.Id}");

            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }
    }
}