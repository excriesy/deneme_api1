using System.ComponentModel.DataAnnotations;
using ShareVault.API.Models;

namespace ShareVault.API.DTOs
{
    public class CreateFolderRequest
    {
        [Required]
        public required string Name { get; set; }
        public string? ParentFolderId { get; set; }
    }

    public class RenameFolderRequest
    {
        [Required]
        public required string NewName { get; set; }
    }

    public class ShareFolderRequest
    {
        [Required]
        public required string SharedWithUserId { get; set; }
        
        [Required]
        public required PermissionType Permission { get; set; }
        
        public DateTime? ExpiresAt { get; set; }
        
        public string? ShareNote { get; set; }
    }

    public class RevokeFolderAccessRequest
    {
        [Required]
        public required string SharedWithUserId { get; set; }
    }
}
