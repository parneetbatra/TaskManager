using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Middleware
{
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = exception switch
            {
                EntityNotFoundException => StatusCodes.Status404NotFound,
                ArgumentException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "An unhandled exception occurred while processing the request.");
            }

            var problemDetails = new ProblemDetails
            {
                Type = statusCode == StatusCodes.Status500InternalServerError ? "https://httpstatuses.com/500" : null,
                Title = statusCode == StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred."
                    : exception.Message,
                Status = statusCode,
                Detail = statusCode == StatusCodes.Status500InternalServerError ? "An error occurred while processing your request." : exception.Message,
                Instance = context.Request.Path
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;
            var payload = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            return context.Response.WriteAsync(payload);
        }
    }
}
