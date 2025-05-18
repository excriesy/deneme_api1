using System;
using System.IO;
using System.Threading.Tasks;

namespace ShareVault.API.Services
{
    public interface ILogService
    {
        Task LogError(string message, Exception ex = null);
        Task LogWarning(string message);
        Task LogInfo(string message);
        Task LogRequest(string method, string path, int statusCode, string message = null);
    }

    public class LogService : ILogService
    {
        private readonly string _logDirectory;
        private readonly object _lockObj = new object();

        public LogService(IWebHostEnvironment environment)
        {
            _logDirectory = Path.Combine(environment.ContentRootPath, "Logs");
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        private async Task WriteLog(string level, string message, Exception ex = null)
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            if (ex != null)
            {
                logMessage += $"\nException: {ex.Message}\nStackTrace: {ex.StackTrace}";
            }

            var logFile = Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.txt");
            
            lock (_lockObj)
            {
                File.AppendAllText(logFile, logMessage + Environment.NewLine);
            }
        }

        public async Task LogError(string message, Exception ex = null)
        {
            await WriteLog("ERROR", message, ex);
        }

        public async Task LogWarning(string message)
        {
            await WriteLog("WARNING", message);
        }

        public async Task LogInfo(string message)
        {
            await WriteLog("INFO", message);
        }

        public async Task LogRequest(string method, string path, int statusCode, string message = null)
        {
            var logMessage = $"{method} {path} - Status: {statusCode}";
            if (!string.IsNullOrEmpty(message))
            {
                logMessage += $" - {message}";
            }

            if (statusCode >= 400)
            {
                await LogError(logMessage);
            }
            else if (statusCode >= 300)
            {
                await LogWarning(logMessage);
            }
            else
            {
                await LogInfo(logMessage);
            }
        }
    }
} 