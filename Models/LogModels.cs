using System;
using System.ComponentModel.DataAnnotations;

namespace ShareVault.API.Models
{
    public class ErrorLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Message { get; set; }
        public string? Exception { get; set; }
        public string? StackTrace { get; set; }
        public string? Source { get; set; }
        public DateTime Timestamp { get; set; }
        public required string Environment { get; set; }
    }

    public class RequestLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Method { get; set; }
        public required string Path { get; set; }
        public int StatusCode { get; set; }
        public string? UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public required string Environment { get; set; }
    }

    public class SecurityLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string EventType { get; set; }
        public required string Details { get; set; }
        public string? UserId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime Timestamp { get; set; }
        public required string Environment { get; set; }
    }
} 