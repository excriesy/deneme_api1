using System;

namespace ShareVault.API.Models
{
    public class SharedFile
    {
        public string Id { get; set; }
        public string FileId { get; set; }
        public string SharedByUserId { get; set; }
        public string SharedWithUserId { get; set; }
        public DateTime SharedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }

        public virtual FileEntity File { get; set; }
        public virtual User SharedByUser { get; set; }
        public virtual User SharedWithUser { get; set; }
    }
} 