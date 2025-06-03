using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ShareVault.API.Data;
using ShareVault.API.Models;
using ShareVault.API.Interfaces;
using Microsoft.Extensions.Logging;

namespace ShareVault.API.Services
{
    public class VersioningService : IVersioningService
    {
        private readonly AppDbContext _context;
        private readonly ILogService _logService;
        private readonly ILogger<VersioningService> _logger;

        public VersioningService(AppDbContext context, ILogService logService, ILogger<VersioningService> logger)
        {
            _context = context;
            _logService = logService;
            _logger = logger;
        }

        public async Task<FileVersion> CreateFileVersionAsync(FileModel file, string userId, string? changeNotes = null)
        {
            var versionNumber = await GetNextFileVersionNumberAsync(file.Id);
            var version = new FileVersion
            {
                Id = Guid.NewGuid().ToString(),
                FileId = file.Id,
                VersionNumber = versionNumber,
                Path = file.Path,
                Size = file.Size,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ChangeNotes = changeNotes
            };

            _context.FileVersions.Add(version);
            await _context.SaveChangesAsync();
            await _logService.LogRequestAsync("POST", $"/api/file/{file.Id}/version", 200, userId);

            return version;
        }

        public async Task<FolderVersion> CreateFolderVersionAsync(Folder folder, string userId, string? changeNotes = null)
        {
            var versionNumber = await GetNextFolderVersionNumberAsync(folder.Id);
            var structureHash = await CalculateFolderStructureHashAsync(folder.Id);
            
            var version = new FolderVersion
            {
                Id = Guid.NewGuid().ToString(),
                FolderId = folder.Id,
                VersionNumber = versionNumber,
                Path = folder.Path,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ChangeNotes = changeNotes,
                StructureHash = structureHash
            };

            _context.FolderVersions.Add(version);
            await _context.SaveChangesAsync();
            await _logService.LogRequestAsync("POST", $"/api/folder/{folder.Id}/version", 200, userId);

            return version;
        }

        public async Task<string> GetNextFileVersionNumberAsync(string fileId)
        {
            var lastVersion = await _context.FileVersions
                .Where(v => v.FileId == fileId)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastVersion == null)
                return "1.0";

            var versionParts = lastVersion.VersionNumber.Split('.');
            var major = int.Parse(versionParts[0]);
            var minor = int.Parse(versionParts[1]);

            return $"{major}.{minor + 1}";
        }

        public async Task<string> GetNextFolderVersionNumberAsync(string folderId)
        {
            var lastVersion = await _context.FolderVersions
                .Where(v => v.FolderId == folderId)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastVersion == null)
                return "1.0";

            var versionParts = lastVersion.VersionNumber.Split('.');
            var major = int.Parse(versionParts[0]);
            var minor = int.Parse(versionParts[1]);

            return $"{major}.{minor + 1}";
        }

        private async Task<string> CalculateFolderStructureHashAsync(string folderId)
        {
            var folder = await _context.Folders
                .Include(f => f.Files)
                .Include(f => f.SubFolders)
                .FirstOrDefaultAsync(f => f.Id == folderId);

            if (folder == null)
                throw new InvalidOperationException("Klasör bulunamadı.");

            var structure = new StringBuilder();
            structure.Append(folder.Name);
            structure.Append(folder.Files.Count);
            structure.Append(folder.SubFolders.Count);

            foreach (var file in folder.Files)
            {
                structure.Append(file.Name);
                structure.Append(file.Size);
                structure.Append(file.LastModified);
            }

            foreach (var subFolder in folder.SubFolders)
            {
                structure.Append(await CalculateFolderStructureHashAsync(subFolder.Id));
            }

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(structure.ToString()));
            return Convert.ToBase64String(hashBytes);
        }

        public async Task<IEnumerable<FileVersion>> GetFileVersionsAsync(string fileId)
        {
            return await _context.FileVersions
                .Where(v => v.FileId == fileId)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<FolderVersion>> GetFolderVersionsAsync(string folderId)
        {
            return await _context.FolderVersions
                .Where(v => v.FolderId == folderId)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }

        public async Task<FileVersion?> GetFileVersionAsync(string fileId, string versionNumber)
        {
            return await _context.FileVersions
                .FirstOrDefaultAsync(v => v.FileId == fileId && v.VersionNumber == versionNumber);
        }

        public async Task<FolderVersion?> GetFolderVersionAsync(string folderId, string versionNumber)
        {
            return await _context.FolderVersions
                .FirstOrDefaultAsync(v => v.FolderId == folderId && v.VersionNumber == versionNumber);
        }
    }
} 