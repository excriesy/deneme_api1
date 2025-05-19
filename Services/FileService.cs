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
using ShareVault.API.DTOs;

namespace ShareVault.API.Services
{
    public class FileService : IFileService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogService _logService;
        private readonly ICacheService _cacheService;
        private readonly string _uploadPath;
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
            _uploadPath = Path.Combine(_environment.ContentRootPath, "Uploads");
            
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        public async Task<string> CompleteUploadAsync(string tempFilePath, string originalFileName, string userId)
        {
            var fileId = Guid.NewGuid().ToString();
            var extension = Path.GetExtension(originalFileName);
            var fileName = $"{fileId}{extension}";
            var filePath = Path.Combine(_uploadPath, fileName);

            System.IO.File.Move(tempFilePath, filePath);

            var file = new FileModel
            {
                Id = fileId,
                Name = originalFileName,
                Path = filePath,
                ContentType = GetContentType(extension),
                Size = new FileInfo(filePath).Length,
                UserId = userId,
                UploadedAt = DateTime.UtcNow
            };

            _context.Files.Add(file);
            await _context.SaveChangesAsync();

            // Clear the cache for this user's file list
            _cacheService.Remove($"user_files_{userId}");

            return fileId;
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

                var filePath = Path.Combine(_uploadPath, fileId + Path.GetExtension(file.Name));
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

                var filePath = Path.Combine(_uploadPath, fileId + Path.GetExtension(file.Name));
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

        public async Task<IEnumerable<FileDto>> ListFilesAsync(string userId)
        {
            await _logService.LogInfoAsync($"ListFilesAsync started for user: {userId}", userId);
            try
            {
                // Check cache first
                var cachedFiles = _cacheService.Get<List<FileDto>>($"user_files_{userId}");
                if (cachedFiles != null)
                {
                    await _logService.LogInfoAsync($"ListFilesAsync returning cached data for user: {userId}", userId);
                    await _logService.LogRequestAsync("GET", "/api/file/list", 200, userId);
                    return cachedFiles;
                }

                // Veritabanından hem kullanıcının kendi dosyalarını hem de paylaşılan dosyaları çek
                await _logService.LogInfoAsync($"Fetching files from database for user: {userId}", userId);
                var filesFromDb = await _context.Files
                    .Include(f => f.UploadedBy)
                    .Where(f => f.UserId == userId)
                    .ToListAsync();
                
                var sharedFilesFromDb = await _context.SharedFiles
                    .Where(sf => sf.SharedWithUserId == userId && sf.IsActive)
                    .Include(sf => sf.File)
                    .ThenInclude(f => f.UploadedBy)
                    .Select(sf => sf.File)
                    .ToListAsync();

                await _logService.LogInfoAsync($"Found {filesFromDb.Count} owned files and {sharedFilesFromDb.Count} shared files in DB for user: {userId}", userId);

                // İki listeyi birleştir ve sadece benzersiz dosyaları al (aynı dosya hem kendi dosyanız hem de paylaşılan olabilir)
                var allFilesFromDb = filesFromDb.Union(sharedFilesFromDb, new FileModelComparer()).ToList();

                await _logService.LogInfoAsync($"Total unique files found in DB: {allFilesFromDb.Count} for user: {userId}", userId);

                var existingFiles = new List<FileDto>();

                // Her dosya kaydının fiziksel dosyasının varlığını kontrol et
                foreach (var file in allFilesFromDb)
                {
                    var filePath = Path.Combine(_uploadPath, file.Id + Path.GetExtension(file.Name));
                    if (System.IO.File.Exists(filePath))
                    {
                        await _logService.LogInfoAsync($"Physical file exists for DB record {file.Id} at {filePath}. Adding to list.", userId);
                        existingFiles.Add(new FileDto
                        {
                            Id = file.Id,
                            Name = file.Name,
                            Size = file.Size,
                            UploadedAt = file.UploadedAt,
                            UploadedBy = file.UploadedBy?.Username ?? "Bilinmeyen Kullanıcı",
                            UserId = file.UserId,
                            ContentType = file.ContentType,
                            Icon = GetFileIcon(file.ContentType),
                            FileType = GetFileType(file.ContentType)
                        });
                    }
                    else
                    {
                        await _logService.LogWarningAsync($"Database record found for file {file.Id}, but physical file does not exist at {filePath}. Skipping.", userId);
                    }
                }
                
                await _logService.LogInfoAsync($"Finished physical file check. {existingFiles.Count} files found to list for user: {userId}", userId);

                // Cache the file list with shorter duration
                _cacheService.Set($"user_files_{userId}", existingFiles, TimeSpan.FromSeconds(30));

                await _logService.LogRequestAsync("GET", "/api/file/list", 200, userId);
                return existingFiles;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error listing files for user {userId}", ex, userId);
                throw;
            }
        }

        public async Task ShareFileAsync(string fileId, string sharedWithUserId, string sharedByUserId)
        {
            try
            {
                var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == sharedByUserId);

                if (file == null)
                    throw new KeyNotFoundException("File not found or not owned by user");

                var sharedFile = new SharedFile
                {
                    Id = Guid.NewGuid().ToString(),
                    FileId = fileId,
                    SharedByUserId = sharedByUserId,
                    SharedWithUserId = sharedWithUserId,
                    SharedAt = DateTime.UtcNow,
                    IsActive = true,
                    File = null!,
                    SharedByUser = null!,
                    SharedWithUser = null!
                };

                _context.SharedFiles.Add(sharedFile);
                await _context.SaveChangesAsync();

                await _logService.LogRequestAsync("POST", $"/api/file/share/{fileId}", 200, sharedByUserId);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error sharing file {fileId}", ex, sharedByUserId);
                throw;
            }
        }

        public async Task RevokeAccessAsync(string fileId, string sharedWithUserId)
        {
            try
            {
                var sharedFile = await _context.SharedFiles
                    .FirstOrDefaultAsync(sf => sf.FileId == fileId && sf.SharedWithUserId == sharedWithUserId && sf.IsActive);

                if (sharedFile == null)
                    throw new KeyNotFoundException("Share not found");

                sharedFile.IsActive = false;
                await _context.SaveChangesAsync();

                await _logService.LogRequestAsync("POST", $"/api/file/revoke-access/{fileId}", 200, sharedFile.SharedByUserId);
            }
            catch (Exception ex)
            {
                // Log the error, but we need the user ID from the file or context, not directly from sharedFile if revoked
                await _logService.LogErrorAsync($"Error revoking access for file {fileId} from user {sharedWithUserId}", ex, "System"); // User ID placeholder
                throw;
            }
        }

        private string GetContentType(string extension)
        {
            return extension.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }

        private string GetFileIcon(string contentType)
        {
            return contentType.ToLower() switch
            {
                var t when t.StartsWith("image/") => "🖼️",
                var t when t.StartsWith("video/") => "🎥",
                var t when t.StartsWith("audio/") => "🎵",
                var t when t.Contains("pdf") => "📄",
                var t when t.Contains("word") => "📝",
                var t when t.Contains("excel") || t.Contains("spreadsheet") => "📊",
                var t when t.Contains("powerpoint") || t.Contains("presentation") => "📑",
                var t when t.Contains("text") => "📃",
                var t when t.Contains("zip") || t.Contains("rar") || t.Contains("7z") => "🗜️",
                _ => "📁"
            };
        }

        private string GetFileType(string contentType)
        {
            return contentType.ToLower() switch
            {
                var t when t.StartsWith("image/") => "Resim",
                var t when t.StartsWith("video/") => "Video",
                var t when t.StartsWith("audio/") => "Ses",
                var t when t.Contains("pdf") => "PDF",
                var t when t.Contains("word") => "Word",
                var t when t.Contains("excel") || t.Contains("spreadsheet") => "Excel",
                var t when t.Contains("powerpoint") || t.Contains("presentation") => "PowerPoint",
                var t when t.Contains("text") => "Metin",
                var t when t.Contains("zip") || t.Contains("rar") || t.Contains("7z") => "Sıkıştırılmış",
                _ => "Dosya"
            };
        }

        private class FileModelComparer : IEqualityComparer<FileModel>
        {
            public bool Equals(FileModel? x, FileModel? y)
            {
                if (x is null && y is null) return true;
                if (x is null || y is null) return false;
                return x.Id.Equals(y.Id);
            }

            public int GetHashCode(FileModel obj)
            {
                if (obj is null) return 0;
                return obj.Id.GetHashCode();
            }
        }
    }
} 