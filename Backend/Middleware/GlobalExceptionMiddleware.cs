using System.Net;
using System.Text.Json;

namespace TaskManagement.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

            var status = ex switch
            {
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                InvalidOperationException => (int)HttpStatusCode.BadRequest,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                _ => (int)HttpStatusCode.InternalServerError
            };

            var message = ex switch
            {
                UnauthorizedAccessException => ex.Message,
                InvalidOperationException => ex.Message,
                KeyNotFoundException => ex.Message,
                _ => "An unexpected error occurred. Please try again later."
            };

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new
            {
                statusCode = status,
                message
            });

            await context.Response.WriteAsync(payload);
        }
    }
}