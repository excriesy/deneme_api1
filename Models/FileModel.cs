using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.API.Models
{
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
        public bool IsPublic { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? FolderId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? UploadedBy { get; set; }

        [ForeignKey("FolderId")]
        public virtual Folder? Folder { get; set; }

        public virtual ICollection<SharedFile> SharedFiles { get; set; } = new List<SharedFile>();
    }
} 