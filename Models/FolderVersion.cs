using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.API.Models
{
    /// <summary>
    /// Klasör versiyonlarını temsil eden model sınıfı
    /// </summary>
    public class FolderVersion
    {
        [Key]
        public required string Id { get; set; }
        
        [Required]
        public required string FolderId { get; set; }
        
        public required string VersionNumber { get; set; }
        
        public required string Path { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public required string UserId { get; set; }
        
        public string? ChangeNotes { get; set; }
        
        public required string StructureHash { get; set; }
        
        [ForeignKey("FolderId")]
        public virtual Folder Folder { get; set; } = null!;
        
        [ForeignKey("UserId")]
        public virtual User CreatedBy { get; set; } = null!;
    }
} 