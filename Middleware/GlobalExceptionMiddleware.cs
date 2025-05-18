using System.Net;
using System.Text.Json;
using ShareVault.API.Services;

namespace ShareVault.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogService _logService;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogService logService)
        {
            _next = next;
            _logService = logService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var response = new { error = new { message = exception.Message } };

            switch (exception)
            {
                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    break;
                case ArgumentException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case KeyNotFoundException:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;
                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            await _logService.LogError($"Global exception handler caught: {exception.Message}", exception);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
} 