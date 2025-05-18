using System;

namespace ShareVault.API.Models
{
    public class FileEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTime UploadDate { get; set; }
        public string UserId { get; set; }
    }
} 