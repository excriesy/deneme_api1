using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShareVault.API.Data;
using ShareVault.API.Interfaces;
using ShareVault.API.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace ShareVault.API.Services
{
    public class LogService : ILogService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LogService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogSecurityAsync(string message, string? userId)
        {
            var logEntry = new LogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Level = "Security",
                Message = message,
                Timestamp = DateTime.UtcNow,
                UserId = userId
            };

            await _context.Logs.AddAsync(logEntry);
            await _context.SaveChangesAsync();
        }

        public async Task LogErrorAsync(string message, Exception exception, string? userId)
        {
            var logEntry = new LogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Level = "Error",
                Message = $"{message}: {exception.Message}",
                Details = exception.ToString(),
                Timestamp = DateTime.UtcNow,
                UserId = userId
            };

            await _context.Logs.AddAsync(logEntry);
            await _context.SaveChangesAsync();
        }

        public async Task LogRequestAsync(string method, string path, int statusCode, string? userId)
        {
            var logEntry = new LogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Level = "Request",
                Message = $"{method} {path} - {statusCode}",
                Timestamp = DateTime.UtcNow,
                UserId = userId
            };

            await _context.Logs.AddAsync(logEntry);
            await _context.SaveChangesAsync();
        }
    }
} 