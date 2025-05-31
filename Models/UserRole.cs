using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.API.Models
{
    /// <summary>
    /// Kullanıcı rol ilişkilerini temsil eden model sınıfı
    /// </summary>
    public class UserRole
    {
        [Key]
        public required string UserId { get; set; }
        
        [Key]
        public required string RoleId { get; set; }
        
        // Rol atama bilgileri
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        
        public string? AssignedBy { get; set; }
        
        public bool IsActive { get; set; } = true;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;
    }
}