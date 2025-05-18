using System;
using System.ComponentModel.DataAnnotations;

namespace ShareVault.API.Models
{
    public class LogEntry
    {
        [Key]
        public required string Id { get; set; }

        [Required]
        public required string Level { get; set; } // ERROR, WARNING, INFO

        [Required]
        public required string Message { get; set; }

        public string? Details { get; set; }

        public string? Exception { get; set; }

        public string? StackTrace { get; set; }

        public string? Source { get; set; } // Controller, Service, Middleware vb.

        public string? UserId { get; set; }

        public string? RequestPath { get; set; }

        public string? RequestMethod { get; set; }

        public int? StatusCode { get; set; }

        public DateTime Timestamp { get; set; }
    }
} 