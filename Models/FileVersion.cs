using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.API.Models
{
    /// <summary>
    /// Dosya versiyonlarını temsil eden model sınıfı
    /// </summary>
    public class FileVersion
    {
        [Key]
        public required string Id { get; set; }
        
        [Required]
        public required string FileId { get; set; }
        
        public required string VersionNumber { get; set; }
        
        public required string Path { get; set; }
        
        public required long Size { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public required string UserId { get; set; }
        
        public string? ChangeNotes { get; set; }
        
        [ForeignKey("FileId")]
        public virtual FileModel File { get; set; } = null!;
        
        [ForeignKey("UserId")]
        public virtual User CreatedBy { get; set; } = null!;
    }
}
