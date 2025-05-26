using ShareVault.API.DTOs;

namespace ShareVault.API.Interfaces
{
    public interface IFolderService
    {
        Task<FolderDto> CreateFolderAsync(string name, string? parentFolderId, string userId);
        Task<IEnumerable<FolderDto>> ListFoldersAsync(string userId, string? parentFolderId = null);
        Task<FolderDto> UpdateFolderAsync(string folderId, string name, string userId);
        Task DeleteFolderAsync(string folderId, string userId);
        Task MoveFolderAsync(string folderId, string? newParentFolderId, string userId);
    }
} 