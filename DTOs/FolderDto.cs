using ShareVault.API.Models;

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
        public string? Description { get; set; }
        public string? Tags { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        public int SharedCount { get; set; }
    }
    
    public class FolderDetailsDto : FolderDto
    {
        public required string Path { get; set; }
        public required string OwnerName { get; set; }
        public string? MetadataText { get; set; }
        public List<FolderDto>? SubFolders { get; set; }
        public List<FileDto>? Files { get; set; }
    }
    
    public class SharedFolderDto
    {
        public required string Id { get; set; }
        public required string FolderId { get; set; }
        public required string FolderName { get; set; }
        public required string SharedByUserId { get; set; }
        public string? SharedByUserName { get; set; }
        public required string SharedWithUserId { get; set; }
        public string? SharedWithUserName { get; set; }
        public required DateTime SharedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public PermissionType Permission { get; set; }
        public bool IsActive { get; set; }
        public string? ShareNote { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public int FileCount { get; set; }
        public int SubFolderCount { get; set; }
    }
}