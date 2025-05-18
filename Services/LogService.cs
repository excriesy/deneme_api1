using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShareVault.API.Data;
using ShareVault.API.Interfaces;
using ShareVault.API.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ShareVault.API.Services
{
    public class LogService : ILogService
    {
        private readonly ILogger<LogService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LogService(ILogger<LogService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogSecurityAsync(string message, string? userId)
        {
            _logger.LogWarning($"Security: {message} - User: {userId}");
            await Task.CompletedTask;
        }

        public async Task LogErrorAsync(string message, Exception exception, string? userId)
        {
            _logger.LogError(exception, $"Error: {message} - User: {userId}");
            await Task.CompletedTask;
        }

        public async Task LogErrorAsync(string message, Exception exception)
        {
            _logger.LogError(exception, message);
            await Task.CompletedTask;
        }

        public async Task LogRequestAsync(string method, string path, int statusCode, string? userId)
        {
            _logger.LogInformation($"Request: {method} {path} - Status: {statusCode} - User: {userId}");
            await Task.CompletedTask;
        }

        public void LogInformation(string message)
        {
            _logger.LogInformation(message);
        }

        public void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }

        public void LogError(string message, Exception? exception = null)
        {
            if (exception != null)
                _logger.LogError(exception, message);
            else
                _logger.LogError(message);
        }

        public void LogDebug(string message)
        {
            _logger.LogDebug(message);
        }
    }
} 