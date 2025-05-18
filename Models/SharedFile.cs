using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.API.Models
{
    public class SharedFile
    {
        [Key]
        public required string Id { get; set; }

        [Required]
        public required string FileId { get; set; }

        [Required]
        public required string SharedByUserId { get; set; }

        [Required]
        public required string SharedWithUserId { get; set; }

        public DateTime SharedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public bool CanEdit { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey("FileId")]
        public virtual required FileModel File { get; set; }

        [ForeignKey("SharedByUserId")]
        public virtual required User SharedByUser { get; set; }

        [ForeignKey("SharedWithUserId")]
        public virtual required User SharedWithUser { get; set; }
    }
} 