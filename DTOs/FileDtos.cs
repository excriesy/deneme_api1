namespace ShareVault.API.DTOs
{
    public class FileDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long Size { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; } = null!;
    }

    public class FileDetailsDto : FileDto
    {
        public string Path { get; set; } = null!;
        public bool IsPublic { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class SharedFileDto
    {
        public string Id { get; set; } = null!;
        public string FileId { get; set; } = null!;
        public string SharedWithUserId { get; set; } = null!;
        public DateTime SharedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool CanEdit { get; set; }
    }
} 