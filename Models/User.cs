using System;
using System.Collections.Generic;

namespace ShareVault.API.Models
{
    public class User
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; }
    }
}
