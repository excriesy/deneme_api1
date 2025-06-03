using ShareVault.API.DTOs;
using ShareVault.API.Models;

namespace ShareVault.API.Interfaces
{
    public interface IFileService
    {
        Task<string> CompleteUploadAsync(string tempFilePath, string originalFileName, string userId, string? folderId = null);
        Task<byte[]> DownloadFileAsync(string fileId, string userId);
        Task<IEnumerable<FileDto>> ListFilesAsync(string userId, string? folderId = null);
        Task<IEnumerable<FileDto>> ListSharedFilesAsync(string userId);
        Task DeleteFileAsync(string fileId, string userId);
        Task<FileModel?> GetFileByIdAsync(string fileId);
        Task ShareFileAsync(string fileId, string sharedWithUserId, string sharedByUserId);
        Task RevokeAccessAsync(string fileId, string sharedWithUserId);
    }
} 