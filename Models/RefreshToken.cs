using System;

namespace ShareVault.API.Models
{
    public class RefreshToken
    {
        public required string Id { get; set; }
        public required string Token { get; set; }
        public required string UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public string? RevokedReason { get; set; }
        public string? ReplacedByToken { get; set; }
        public required User User { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
} 