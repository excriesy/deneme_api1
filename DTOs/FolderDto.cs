namespace ShareVault.API.DTOs
{
    public class FolderDto
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public string? ParentFolderId { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required string UserId { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int FileCount { get; set; }
        public int SubFolderCount { get; set; }
    }
} 