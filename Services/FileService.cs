using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ShareVault.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ShareVault.API.Services;
using ShareVault.API.Models;

namespace ShareVault.API.Services
{
    public interface IFileService
    {
        Task<string> UploadFileAsync(IFormFile file, string userId);
        Task<byte[]> DownloadFileAsync(string fileId, string userId);
        Task DeleteFileAsync(string fileId, string userId);
        Task<List<FileEntity>> ListFilesAsync(string userId);
    }

    public class FileService : IFileService
    {
        private readonly AppDbContext _context;
        private readonly ILogService _logService;
        private readonly ICacheService _cacheService;
        private const int ChunkSize = 1024 * 1024; // 1MB chunks

        public FileService(
            AppDbContext context,
            ILogService logService,
            ICacheService cacheService)
        {
            _context = context;
            _logService = logService;
            _cacheService = cacheService;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string userId)
        {
            try
            {
                var fileId = Guid.NewGuid().ToString();
                var filePath = Path.Combine("Uploads", fileId);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    var buffer = new byte[ChunkSize];
                    int bytesRead;
                    while ((bytesRead = await file.OpenReadStream().ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await stream.WriteAsync(buffer, 0, bytesRead);
                    }
                }

                var fileEntity = new FileEntity
                {
                    Id = fileId,
                    Name = file.FileName,
                    Size = file.Length,
                    UploadDate = DateTime.UtcNow,
                    UserId = userId
                };

                _context.Files.Add(fileEntity);
                await _context.SaveChangesAsync();

                // Cache the file metadata
                _cacheService.Set($"file_meta_{fileId}", fileEntity, TimeSpan.FromMinutes(30));

                await _logService.LogInfo($"File uploaded: {file.FileName} by user {userId}");
                return fileId;
            }
            catch (Exception ex)
            {
                await _logService.LogError($"Error uploading file: {file.FileName}", ex);
                throw;
            }
        }

        public async Task<byte[]> DownloadFileAsync(string fileId, string userId)
        {
            try
            {
                // Check cache first
                var cachedFile = _cacheService.Get<byte[]>($"file_content_{fileId}");
                if (cachedFile != null)
                {
                    await _logService.LogInfo($"File served from cache: {fileId}");
                    return cachedFile;
                }

                var file = await _context.Files
                    .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);

                if (file == null)
                    throw new KeyNotFoundException("File not found");

                var filePath = Path.Combine("Uploads", fileId);
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("File not found on disk");

                var fileBytes = await File.ReadAllBytesAsync(filePath);

                // Cache the file content
                _cacheService.Set($"file_content_{fileId}", fileBytes, TimeSpan.FromMinutes(5));

                await _logService.LogInfo($"File downloaded: {file.Name} by user {userId}");
                return fileBytes;
            }
            catch (Exception ex)
            {
                await _logService.LogError($"Error downloading file: {fileId}", ex);
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

                var filePath = Path.Combine("Uploads", fileId);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                _context.Files.Remove(file);
                await _context.SaveChangesAsync();

                // Remove from cache
                _cacheService.Remove($"file_meta_{fileId}");
                _cacheService.Remove($"file_content_{fileId}");

                await _logService.LogInfo($"File deleted: {file.Name} by user {userId}");
            }
            catch (Exception ex)
            {
                await _logService.LogError($"Error deleting file: {fileId}", ex);
                throw;
            }
        }

        public async Task<List<FileEntity>> ListFilesAsync(string userId)
        {
            try
            {
                // Check cache first
                var cachedFiles = _cacheService.Get<List<FileEntity>>($"user_files_{userId}");
                if (cachedFiles != null)
                {
                    await _logService.LogInfo($"File list served from cache for user {userId}");
                    return cachedFiles;
                }

                var files = await _context.Files
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.UploadDate)
                    .ToListAsync();

                // Cache the file list
                _cacheService.Set($"user_files_{userId}", files, TimeSpan.FromMinutes(5));

                await _logService.LogInfo($"File list retrieved for user {userId}");
                return files;
            }
            catch (Exception ex)
            {
                await _logService.LogError($"Error listing files for user {userId}", ex);
                throw;
            }
        }
    }
} 