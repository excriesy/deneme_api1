using ShareVault.API.DTOs;

namespace ShareVault.API.Interfaces
{
    public interface IFileService
    {
        Task<string> CompleteUploadAsync(string tempFilePath, string originalFileName, string userId, string? folderId);
        Task<byte[]> DownloadFileAsync(string fileId, string userId);
        Task DeleteFileAsync(string fileId, string userId);
        Task<IEnumerable<FileDto>> ListFilesAsync(string userId, string? parentFolderId = null);
        Task ShareFileAsync(string fileId, string sharedWithUserId, string sharedByUserId);
        Task RevokeAccessAsync(string fileId, string sharedWithUserId);
        Task<IEnumerable<FileDto>> ListSharedFilesAsync(string userId);
    }
} 