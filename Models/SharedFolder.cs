using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.API.Models
{
    /// <summary>
    /// Klasör paylaşımlarını temsil eden model sınıfı
    /// </summary>
    public class SharedFolder
    {
        [Key]
        public required string Id { get; set; }
        
        [Required]
        public required string FolderId { get; set; }
        
        [Required]
        public required string SharedByUserId { get; set; }
        
        [Required]
        public required string SharedWithUserId { get; set; }
        
        public DateTime SharedAt { get; set; }
        
        public DateTime? ExpiresAt { get; set; }
        
        // İzin türü - varsayılan olarak sadece okuma hakkı vererek
        public PermissionType Permission { get; set; } = PermissionType.Read;
        
        public bool IsActive { get; set; } = true;
        
        // Paylaşımın neden/açıklama alanı
        public string? ShareNote { get; set; }
        
        // Paylaşım log bilgileri
        public string? LastAccessedBy { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public int AccessCount { get; set; } = 0;

        [ForeignKey("FolderId")]
        public virtual required Folder Folder { get; set; }

        [ForeignKey("SharedByUserId")]
        public virtual required User SharedByUser { get; set; }

        [ForeignKey("SharedWithUserId")]
        public virtual required User SharedWithUser { get; set; }
    }
}
