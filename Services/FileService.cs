using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ShareVault.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ShareVault.API.Services;
using ShareVault.API.Models;
using Microsoft.AspNetCore.Hosting;
using ShareVault.API.Interfaces;

namespace ShareVault.API.Services
{
    public interface IFileService
    {
        Task<string> UploadFileAsync(IFormFile file, string userId);
        Task<byte[]> DownloadFileAsync(string fileId, string userId);
        Task DeleteFileAsync(string fileId, string userId);
        Task<IEnumerable<FileModel>> ListFilesAsync(string userId);
    }

    public class FileService : IFileService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogService _logService;
        private readonly ICacheService _cacheService;
        private const int ChunkSize = 1024 * 1024; // 1MB chunks

        public FileService(
            AppDbContext context,
            IWebHostEnvironment environment,
            ILogService logService,
            ICacheService cacheService)
        {
            _context = context;
            _environment = environment;
            _logService = logService;
            _cacheService = cacheService;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string userId)
        {
            var fileModel = new FileModel
            {
                Id = Guid.NewGuid().ToString(),
                Name = file.FileName,
                ContentType = file.ContentType,
                Size = file.Length,
                Path = Path.Combine("uploads", userId, file.FileName),
                UserId = userId,
                UploadedAt = DateTime.UtcNow,
                IsPublic = false
            };

            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", userId);
            Directory.CreateDirectory(uploadPath);

            using (var stream = new FileStream(Path.Combine(_environment.WebRootPath, fileModel.Path), FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _context.Files.Add(fileModel);
            await _context.SaveChangesAsync();

            // Cache the file metadata
            _cacheService.Set($"file_meta_{fileModel.Id}", fileModel, TimeSpan.FromMinutes(30));

            await _logService.LogRequestAsync("POST", $"/api/file/upload/{fileModel.Id}", 200, userId);
            return fileModel.Id;
        }

        public async Task<byte[]> DownloadFileAsync(string fileId, string userId)
        {
            try
            {
                // Check cache first
                var cachedFile = _cacheService.Get<byte[]>($"file_content_{fileId}");
                if (cachedFile != null)
                {
                    await _logService.LogRequestAsync("GET", $"/api/file/download/{fileId}", 200, userId);
                    return cachedFile;
                }

                var file = await _context.Files
                    .FirstOrDefaultAsync(f => f.Id == fileId);

                if (file == null)
                    throw new KeyNotFoundException("File not found");

                if (file.UserId != userId && !file.IsPublic)
                {
                    var hasAccess = await _context.SharedFiles
                        .AnyAsync(sf => sf.FileId == fileId && sf.SharedWithUserId == userId && sf.IsActive);

                    if (!hasAccess)
                        throw new UnauthorizedAccessException("Bu dosyaya erişim izniniz yok");
                }

                var filePath = Path.Combine(_environment.WebRootPath, file.Path);
                if (!System.IO.File.Exists(filePath))
                    throw new FileNotFoundException("File not found on disk");

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

                // Cache the file content
                _cacheService.Set($"file_content_{fileId}", fileBytes, TimeSpan.FromMinutes(5));

                await _logService.LogRequestAsync("GET", $"/api/file/download/{fileId}", 200, userId);
                return fileBytes;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error downloading file: {fileId}", ex, userId);
                throw;
            }
        }

        public async Task DeleteFileAsync(string fileId, string userId)
        {
            try
            {
                var file = await _context.Files
                    .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);

                if (file == null)
                    throw new KeyNotFoundException("File not found");

                var filePath = Path.Combine(_environment.WebRootPath, file.Path);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _context.Files.Remove(file);
                await _context.SaveChangesAsync();

                // Remove from cache
                _cacheService.Remove($"file_meta_{fileId}");
                _cacheService.Remove($"file_content_{fileId}");

                await _logService.LogRequestAsync("DELETE", $"/api/file/{fileId}", 200, userId);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error deleting file: {fileId}", ex, userId);
                throw;
            }
        }

        public async Task<IEnumerable<FileModel>> ListFilesAsync(string userId)
        {
            try
            {
                // Check cache first
                var cachedFiles = _cacheService.Get<IEnumerable<FileModel>>($"user_files_{userId}");
                if (cachedFiles != null)
                {
                    await _logService.LogRequestAsync("GET", "/api/file/list", 200, userId);
                    return cachedFiles;
                }

                var userFiles = await _context.Files
                    .Where(f => f.UserId == userId)
                    .ToListAsync();

                var sharedFiles = await _context.SharedFiles
                    .Where(sf => sf.SharedWithUserId == userId && sf.IsActive)
                    .Include(sf => sf.File)
                    .Select(sf => sf.File)
                    .ToListAsync();

                var allFiles = userFiles.Concat(sharedFiles);

                // Cache the file list
                _cacheService.Set($"user_files_{userId}", allFiles, TimeSpan.FromMinutes(5));

                await _logService.LogRequestAsync("GET", "/api/file/list", 200, userId);
                return allFiles;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error listing files for user {userId}", ex, userId);
                throw;
            }
        }
    }
} 