using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.API.Models
{
    public class User
    {
        [Key]
        public required string Id { get; set; }

        [Required]
        public required string Username { get; set; }

        [Required]
        public required string Email { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual required ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        public virtual required ICollection<FileModel> Files { get; set; } = new List<FileModel>();
    }
}
