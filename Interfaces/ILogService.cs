using System;
using System.Threading.Tasks;

namespace ShareVault.API.Interfaces
{
    public interface ILogService
    {
        Task LogSecurityAsync(string message, string? userId);
        Task LogErrorAsync(string message, Exception exception, string? userId);
        Task LogRequestAsync(string method, string path, int statusCode, string? userId);
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message, Exception? exception = null);
        void LogDebug(string message);
        Task LogErrorAsync(string message, Exception exception);
    }
} 