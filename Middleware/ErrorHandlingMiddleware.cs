using Microsoft.AspNetCore.Http;
using ShareVault.API.Services;
using ShareVault.API.Interfaces;
using System.Net;
using System.Text.Json;

namespace ShareVault.API.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogService _logService;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogService logService)
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

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.",
                DetailedMessage = ex.Message
            };

            switch (ex)
            {
                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse = new
                    {
                        StatusCode = response.StatusCode,
                        Message = "Bu işlem için yetkiniz bulunmuyor.",
                        DetailedMessage = "Lütfen giriş yapın veya yetkilerinizi kontrol edin."
                    };
                    break;

                case KeyNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse = new
                    {
                        StatusCode = response.StatusCode,
                        Message = "İstenen kaynak bulunamadı.",
                        DetailedMessage = ex.Message
                    };
                    break;

                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = new
                    {
                        StatusCode = response.StatusCode,
                        Message = "Geçersiz işlem.",
                        DetailedMessage = ex.Message
                    };
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            // Hatayı logla
            await _logService.LogErrorAsync($"HTTP {response.StatusCode} - {context.Request.Method} {context.Request.Path}", ex);

            // İsteği logla
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            await _logService.LogRequestAsync(context.Request.Method, context.Request.Path, response.StatusCode, userId);

            var result = JsonSerializer.Serialize(errorResponse);
            await response.WriteAsync(result);
        }
    }
} 