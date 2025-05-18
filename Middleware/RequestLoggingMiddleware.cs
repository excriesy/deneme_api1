 using System.Diagnostics;
using ShareVault.API.Services;

namespace ShareVault.API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogService _logService;

        public RequestLoggingMiddleware(RequestDelegate next, ILogService logService)
        {
            _next = next;
            _logService = logService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            var originalBodyStream = context.Response.Body;

            try
            {
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                await _next(context);

                sw.Stop();
                var elapsed = sw.ElapsedMilliseconds;

                var statusCode = context.Response.StatusCode;
                var method = context.Request.Method;
                var path = context.Request.Path;

                await _logService.LogRequest(
                    method,
                    path,
                    statusCode,
                    $"Elapsed: {elapsed}ms"
                );

                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                await _logService.LogError($"Request failed: {context.Request.Method} {context.Request.Path}", ex);
                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
    }
}