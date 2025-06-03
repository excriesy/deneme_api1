# ShareVault API - Klasör Paylaşım ve Versiyonlama Düzeltmeleri

ShareVault API için klasör paylaşım sistemini ve dosya versiyonlama altyapısını ekleyeceğim. İşte detaylı çözüm:

## 1. FolderPermission Model

**Dosya: `Models/FolderPermission.cs`**

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.Models
{
    public class FolderPermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FolderId { get; set; }

        [ForeignKey("FolderId")]
        public virtual Folder Folder { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        public PermissionType Permission { get; set; }

        public bool CanShare { get; set; } = false;

        public bool InheritToSubfolders { get; set; } = true;

        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiresAt { get; set; }

        public int? GrantedByUserId { get; set; }

        [ForeignKey("GrantedByUserId")]
        public virtual User GrantedByUser { get; set; }

        public bool IsActive => ExpiresAt == null || ExpiresAt > DateTime.UtcNow;
    }

    public enum PermissionType
    {
        Read = 1,
        Write = 2,
        Delete = 3,
        Owner = 4
    }
}
```

## 2. FolderShareLink Model

**Dosya: `Models/FolderShareLink.cs`**

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.Models
{
    public class FolderShareLink
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FolderId { get; set; }

        [ForeignKey("FolderId")]
        public virtual Folder Folder { get; set; }

        [Required]
        [StringLength(100)]
        public string Token { get; set; }

        [Required]
        public int CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedByUser { get; set; }

        public PermissionType Permission { get; set; } = PermissionType.Read;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiresAt { get; set; }

        public int? MaxUses { get; set; }

        public int UsageCount { get; set; } = 0;

        public bool RequiresPassword { get; set; } = false;

        public string PasswordHash { get; set; }

        public bool IsActive => 
            (ExpiresAt == null || ExpiresAt > DateTime.UtcNow) && 
            (MaxUses == null || UsageCount < MaxUses);
    }
}
```

## 3. Gelişmiş Folder Model

**Dosya: `Models/Folder.cs` (Güncelleme)**

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.Models
{
    public class Folder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        public int? ParentFolderId { get; set; }

        [ForeignKey("ParentFolderId")]
        public virtual Folder ParentFolder { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsShared { get; set; } = false;

        public bool InheritParentPermissions { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<Folder> SubFolders { get; set; } = new List<Folder>();
        public virtual ICollection<File> Files { get; set; } = new List<File>();
        public virtual ICollection<FolderPermission> Permissions { get; set; } = new List<FolderPermission>();
        public virtual ICollection<FolderShareLink> ShareLinks { get; set; } = new List<FolderShareLink>();

        // Computed Properties
        [NotMapped]
        public string FullPath
        {
            get
            {
                var path = Name;
                var parent = ParentFolder;
                while (parent != null)
                {
                    path = $"{parent.Name}/{path}";
                    parent = parent.ParentFolder;
                }
                return path;
            }
        }

        [NotMapped]
        public long TotalSize => CalculateTotalSize();

        private long CalculateTotalSize()
        {
            long size = 0;
            foreach (var file in Files)
            {
                size += file.Size;
            }
            foreach (var subFolder in SubFolders)
            {
                size += subFolder.TotalSize;
            }
            return size;
        }
    }
}
```

## 4. FileVersion Model

**Dosya: `Models/FileVersion.cs`**

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.Models
{
    public class FileVersion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FileId { get; set; }

        [ForeignKey("FileId")]
        public virtual File File { get; set; }

        [Required]
        public int VersionNumber { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }

        [Required]
        public long Size { get; set; }

        [StringLength(100)]
        public string ContentHash { get; set; }

        public int UploadedByUserId { get; set; }

        [ForeignKey("UploadedByUserId")]
        public virtual User UploadedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(1000)]
        public string Comment { get; set; }

        public bool IsActive { get; set; } = false;

        // Metadata
        public string Metadata { get; set; } // JSON string for additional info
    }
}
```

## 5. File Model Güncelleme

**Dosya: `Models/File.cs` (Güncelleme)**

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.Models
{
    public class File
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        public long Size { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }

        [StringLength(100)]
        public string ContentType { get; set; }

        [StringLength(100)]
        public string ContentHash { get; set; }

        [Required]
        public int FolderId { get; set; }

        [ForeignKey("FolderId")]
        public virtual Folder Folder { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int CurrentVersionNumber { get; set; } = 1;

        public bool EnableVersioning { get; set; } = true;

        public int? MaxVersions { get; set; } = 10;

        // Navigation Properties
        public virtual ICollection<FileVersion> Versions { get; set; } = new List<FileVersion>();
        public virtual ICollection<FileShare> FileShares { get; set; } = new List<FileShare>();
    }
}
```

## 6. Enhanced FolderService

**Dosya: `Services/FolderService.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShareVault.Data;
using ShareVault.Models;
using ShareVault.DTOs;
using System.Security.Cryptography;

namespace ShareVault.Services
{
    public interface IFolderService
    {
        Task<Folder> CreateFolderAsync(CreateFolderDto dto, int userId);
        Task<Folder> GetFolderAsync(int folderId, int userId);
        Task<IEnumerable<Folder>> GetUserFoldersAsync(int userId);
        Task<IEnumerable<Folder>> GetSharedFoldersAsync(int userId);
        Task<bool> DeleteFolderAsync(int folderId, int userId);
        Task<FolderPermission> ShareFolderAsync(int folderId, int ownerUserId, ShareFolderDto dto);
        Task<FolderShareLink> CreateShareLinkAsync(int folderId, int userId, CreateShareLinkDto dto);
        Task<Folder> GetFolderByShareTokenAsync(string token, string password = null);
        Task<bool> HasPermissionAsync(int folderId, int userId, PermissionType requiredPermission);
        Task<IEnumerable<FolderPermission>> GetFolderPermissionsAsync(int folderId, int userId);
        Task<bool> UpdatePermissionAsync(int permissionId, int userId, UpdatePermissionDto dto);
        Task<bool> RevokePermissionAsync(int permissionId, int userId);
    }

    public class FolderService : IFolderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FolderService> _logger;

        public FolderService(ApplicationDbContext context, ILogger<FolderService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Folder> CreateFolderAsync(CreateFolderDto dto, int userId)
        {
            // Check parent folder permissions if exists
            if (dto.ParentFolderId.HasValue)
            {
                var hasPermission = await HasPermissionAsync(dto.ParentFolderId.Value, userId, PermissionType.Write);
                if (!hasPermission)
                {
                    throw new UnauthorizedAccessException("No write permission on parent folder");
                }
            }

            var folder = new Folder
            {
                Name = dto.Name,
                Description = dto.Description,
                ParentFolderId = dto.ParentFolderId,
                UserId = userId,
                InheritParentPermissions = dto.InheritParentPermissions ?? true
            };

            _context.Folders.Add(folder);
            await _context.SaveChangesAsync();

            // Add owner permission
            var ownerPermission = new FolderPermission
            {
                FolderId = folder.Id,
                UserId = userId,
                Permission = PermissionType.Owner,
                CanShare = true,
                InheritToSubfolders = true,
                GrantedByUserId = userId
            };

            _context.FolderPermissions.Add(ownerPermission);
            await _context.SaveChangesAsync();

            return folder;
        }

        public async Task<Folder> GetFolderAsync(int folderId, int userId)
        {
            var folder = await _context.Folders
                .Include(f => f.Files)
                .Include(f => f.SubFolders)
                .Include(f => f.Permissions)
                .FirstOrDefaultAsync(f => f.Id == folderId);

            if (folder == null)
                return null;

            var hasPermission = await HasPermissionAsync(folderId, userId, PermissionType.Read);
            if (!hasPermission)
                throw new UnauthorizedAccessException("No read permission");

            return folder;
        }

        public async Task<IEnumerable<Folder>> GetUserFoldersAsync(int userId)
        {
            return await _context.Folders
                .Where(f => f.UserId == userId && f.ParentFolderId == null)
                .Include(f => f.SubFolders)
                .Include(f => f.Files)
                .ToListAsync();
        }

        public async Task<IEnumerable<Folder>> GetSharedFoldersAsync(int userId)
        {
            var sharedFolderIds = await _context.FolderPermissions
                .Where(fp => fp.UserId == userId && fp.Folder.UserId != userId && fp.IsActive)
                .Select(fp => fp.FolderId)
                .Distinct()
                .ToListAsync();

            return await _context.Folders
                .Where(f => sharedFolderIds.Contains(f.Id))
                .Include(f => f.User)
                .ToListAsync();
        }

        public async Task<bool> DeleteFolderAsync(int folderId, int userId)
        {
            var hasPermission = await HasPermissionAsync(folderId, userId, PermissionType.Delete);
            if (!hasPermission)
                return false;

            var folder = await _context.Folders
                .Include(f => f.SubFolders)
                .Include(f => f.Files)
                .FirstOrDefaultAsync(f => f.Id == folderId);

            if (folder == null)
                return false;

            // Recursively delete subfolders
            foreach (var subFolder in folder.SubFolders.ToList())
            {
                await DeleteFolderAsync(subFolder.Id, userId);
            }

            _context.Folders.Remove(folder);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<FolderPermission> ShareFolderAsync(int folderId, int ownerUserId, ShareFolderDto dto)
        {
            var hasSharePermission = await _context.FolderPermissions
                .AnyAsync(fp => fp.FolderId == folderId && fp.UserId == ownerUserId && 
                         fp.CanShare && fp.IsActive);

            if (!hasSharePermission)
                throw new UnauthorizedAccessException("No share permission");

            // Check if permission already exists
            var existingPermission = await _context.FolderPermissions
                .FirstOrDefaultAsync(fp => fp.FolderId == folderId && fp.UserId == dto.UserId);

            if (existingPermission != null)
            {
                existingPermission.Permission = dto.Permission;
                existingPermission.CanShare = dto.CanShare;
                existingPermission.ExpiresAt = dto.ExpiresAt;
                existingPermission.InheritToSubfolders = dto