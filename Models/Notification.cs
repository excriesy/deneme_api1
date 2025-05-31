using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.API.Models
{
    /// <summary>
    /// Kullanıcı bildirimlerini temsil eden model sınıfı
    /// </summary>
    public class Notification
    {
        [Key]
        public required string Id { get; set; }
        
        [Required]
        public required string UserId { get; set; }
        
        [Required]
        public required string Message { get; set; }
        
        public string? ActionLink { get; set; }
        
        public NotificationType Type { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public bool IsRead { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
