using ShareVault.API.Models;
using System.ComponentModel.DataAnnotations;

namespace ShareVault.API.DTOs
{
    /// <summary>
    /// Tek bir kullanıcıyla dosya paylaşımı için istek modeli
    /// </summary>
    public class ShareFileRequest
    {
        /// <summary>
        /// Paylaşılacak dosyanın ID'si
        /// </summary>
        [Required]
        public required string FileId { get; set; }

        /// <summary>
        /// Dosyanın paylaşılacağı kullanıcının ID'si
        /// </summary>
        [Required]
        public required string SharedWithUserId { get; set; }

        /// <summary>
        /// Paylaşım izin tipi (varsayılan: Read)
        /// </summary>
        public PermissionType? PermissionType { get; set; } = Models.PermissionType.Read;

        /// <summary>
        /// Paylaşımın sona erme tarihi (opsiyonel)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Paylaşım notu (opsiyonel)
        /// </summary>
        public string? ShareNote { get; set; }
    }
}
