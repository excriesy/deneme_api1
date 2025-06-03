using System.Collections.Generic;
using System.Threading.Tasks;
using ShareVault.API.Models;

namespace ShareVault.API.Interfaces
{
    public interface IVersioningService
    {
        Task<FileVersion> CreateFileVersionAsync(FileModel file, string userId, string? changeNotes = null);
        Task<FolderVersion> CreateFolderVersionAsync(Folder folder, string userId, string? changeNotes = null);
        Task<string> GetNextFileVersionNumberAsync(string fileId);
        Task<string> GetNextFolderVersionNumberAsync(string folderId);
        Task<IEnumerable<FileVersion>> GetFileVersionsAsync(string fileId);
        Task<IEnumerable<FolderVersion>> GetFolderVersionsAsync(string folderId);
        Task<FileVersion?> GetFileVersionAsync(string fileId, string versionNumber);
        Task<FolderVersion?> GetFolderVersionAsync(string folderId, string versionNumber);
    }
} 