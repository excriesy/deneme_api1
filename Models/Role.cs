using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ShareVault.API.Models
{
    /// <summary>
    /// Kullanıcı rollerini temsil eden model sınıfı
    /// </summary>
    public class Role
    {
        [Key]
        public required string Id { get; set; }

        public required string Name { get; set; }
        
        public string? Description { get; set; }
        
        public bool IsSystemRole { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public string? CreatedBy { get; set; }
        
        // İlişkiler
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
