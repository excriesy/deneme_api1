using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.API.Models
{
    /// <summary>
    /// Klasörleri temsil eden model sınıfı
    /// </summary>
    public class Folder
    {
        [Key]
        public required string Id { get; set; }

        [Required]
        public required string Name { get; set; }

        [Required]
        public required string UserId { get; set; }

        public string? ParentFolderId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Çöp kutusu özellikleri
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        // Metadata özellikleri
        public string? Description { get; set; }
        public string? Tags { get; set; }
        public string? MetadataText { get; set; }

        [ForeignKey("UserId")]
        public virtual User Owner { get; set; } = null!;

        [ForeignKey("ParentFolderId")]
        public virtual Folder? ParentFolder { get; set; }

        public virtual ICollection<Folder> SubFolders { get; set; } = new List<Folder>();

        public virtual ICollection<FileModel> Files { get; set; } = new List<FileModel>();

        // Klasör paylaşımları
        public virtual ICollection<SharedFolder> SharedFolders { get; set; } = new List<SharedFolder>();
    }
}