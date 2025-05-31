using ShareVault.API.DTOs;
using ShareVault.API.Models;

namespace ShareVault.API.Interfaces
{
    public interface IFolderService
    {
        // Mevcut klasör yönetim metodları
        Task<FolderDto> CreateFolderAsync(string name, string? parentFolderId, string userId);
        Task<IEnumerable<FolderDto>> ListFoldersAsync(string userId, string? parentFolderId = null);
        Task<FolderDto> UpdateFolderAsync(string folderId, string name, string userId);
        Task DeleteFolderAsync(string folderId, string userId);
        Task MoveFolderAsync(string folderId, string? newParentFolderId, string userId);
        
        // Klasör paylaşım metodları
        Task<SharedFolderDto> ShareFolderAsync(string folderId, string sharedByUserId, string sharedWithUserId, PermissionType permission = PermissionType.Read, DateTime? expiresAt = null, string? shareNote = null);
        Task<IEnumerable<SharedFolderDto>> GetSharedFoldersAsync(string userId);
        Task<IEnumerable<SharedFolderDto>> GetFolderSharesAsync(string folderId, string userId);
        Task RevokeFolderAccessAsync(string folderId, string sharedWithUserId, string userId);
        Task<bool> HasAccessToFolderAsync(string folderId, string userId, PermissionType requiredPermission = PermissionType.Read);
    }
}