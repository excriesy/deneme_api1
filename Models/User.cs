using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.API.Models
{
    /// <summary>
    /// Kullanıcıları temsil eden model sınıfı
    /// </summary>
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
        
        // Kullanıcı profil bilgileri
        public string? FullName { get; set; }
        public string? ProfileImage { get; set; }
        public string? Department { get; set; }
        public string? PhoneNumber { get; set; }

        // Kullanıcı hesap bilgileri
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? LastLoginIP { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsEmailVerified { get; set; } = false;
        public DateTime? EmailVerifiedAt { get; set; }
        
        // Kullanıcı depolama bilgileri (10GB varsayılan)
        public long StorageQuota { get; set; } = 10L * 1024L * 1024L * 1024L; 
        public long StorageUsed { get; set; } = 0L;

        // İlişkiler
        public virtual required ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual required ICollection<FileModel> Files { get; set; } = new List<FileModel>();
        
        // Bildirimler
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
