using System.ComponentModel.DataAnnotations;

namespace ShareVault.API.DTOs
{
    public class FileDto
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string ContentType { get; set; }
        public required long Size { get; set; }
        public required DateTime UploadedAt { get; set; }
        public required string UploadedBy { get; set; }
        public required string UserId { get; set; }
        public string? Icon { get; set; }
        public string? FileType { get; set; }
        public bool IsPreviewable { get; set; }
        public string? FolderId { get; set; }
    }

    public class FileDetailsDto : FileDto
    {
        public required string Path { get; set; }
        public required bool IsPublic { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class SharedFileDto
    {
        public required string Id { get; set; }
        public required string FileId { get; set; }
        public required string SharedWithUserId { get; set; }
        public required DateTime SharedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public required bool CanEdit { get; set; }
    }

    public class CompleteUploadRequest
    {
        [Required]
        public required string TempFileName { get; set; }

        [Required]
        public required string OriginalFileName { get; set; }
        public string? FolderId { get; set; }
    }

    public class CancelUploadRequest
    {
        [Required]
        public required string TempFileName { get; set; }
    }

    public class BulkDeleteRequest
    {
        [Required]
        public required List<string> FileIds { get; set; }
    }

    public class BulkShareRequest
    {
        [Required]
        public required List<string> FileIds { get; set; }

        [Required]
        public required List<string> UserIds { get; set; }
    }

    public class BulkOperationResult
    {
        public required string FileId { get; set; }
        public bool Success { get; set; }
        public required string Message { get; set; }
    }

    public class BulkShareResult
    {
        public required string FileId { get; set; }
        public required string FileName { get; set; }
        public required List<ShareResult> ShareResults { get; set; }
    }

    public class ShareMultipleRequest
    {
        [Required]
        public required string FileId { get; set; }

        [Required]
        public required List<string> UserIds { get; set; }
    }

    public class ShareResult
    {
        public required string UserId { get; set; }
        public bool Success { get; set; }
        public required string Message { get; set; }
    }
} 