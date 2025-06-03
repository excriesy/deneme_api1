using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.API.Models
{
    /// <summary>
    /// Dosyaları temsil eden model sınıfı
    /// </summary>
    public class FileModel
    {
        [Key]
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string ContentType { get; set; }
        public required long Size { get; set; }
        public required string Path { get; set; }
        public required string UserId { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsPublic { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? FolderId { get; set; }
        
        // Şifreleme özellikleri
        public bool IsEncrypted { get; set; }
        public string? EncryptionMethod { get; set; }
        public string? EncryptionKey { get; set; }
        
        // Metadata özellikleri
        public string? MetadataText { get; set; }
        public string? Tags { get; set; }
        public string? Description { get; set; }
        
        // Çöp kutusu özellikleri
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        [ForeignKey("UserId")]
        public virtual User? UploadedBy { get; set; }

        [ForeignKey("FolderId")]
        public virtual Folder? Folder { get; set; }

        public virtual ICollection<SharedFile> SharedFiles { get; set; } = new List<SharedFile>();
        
        // Dosya versiyonları
        public virtual ICollection<FileVersion> Versions { get; set; } = new List<FileVersion>();
    }
}