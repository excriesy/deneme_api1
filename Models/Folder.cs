using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.API.Models
{
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

        [ForeignKey("UserId")]
        public virtual User? Owner { get; set; }

        [ForeignKey("ParentFolderId")]
        public virtual Folder? ParentFolder { get; set; }

        public virtual ICollection<Folder> SubFolders { get; set; } = new List<Folder>();

        public virtual ICollection<FileModel> Files { get; set; } = new List<FileModel>();
    }
} 