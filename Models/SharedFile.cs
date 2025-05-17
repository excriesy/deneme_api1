using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.API.Models
{
    public class SharedFile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FileId { get; set; }

        [ForeignKey("FileId")]
        public FileModel File { get; set; }

        [Required]
        public int SharedWithUserId { get; set; }

        [ForeignKey("SharedWithUserId")]
        public User SharedWithUser { get; set; }

        [Required]
        public DateTime SharedDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
} 