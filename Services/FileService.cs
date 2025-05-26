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

        public async Task<string> CompleteUploadAsync(string tempFilePath, string originalFileName, string userId, string? folderId)
        {
            try
            {
                await _logService.LogInfoAsync($"Dosya yükleme başlatılıyor. Temp dosya: {tempFilePath}, Orijinal dosya: {originalFileName}", userId);

                if (!System.IO.File.Exists(tempFilePath))
                {
                    await _logService.LogErrorAsync($"Geçici dosya bulunamadı: {tempFilePath}", new FileNotFoundException($"Geçici dosya bulunamadı: {tempFilePath}"), userId);
                    throw new FileNotFoundException($"Geçici dosya bulunamadı: {tempFilePath}");
                }

                var fileId = Guid.NewGuid().ToString();
                var extension = Path.GetExtension(originalFileName);
                var fileName = $"{fileId}{extension}";
                var filePath = Path.Combine(_uploadPath, fileName);

                await _logService.LogInfoAsync($"Dosya yolu oluşturuldu: {filePath}", userId);

                try
                {
                    System.IO.File.Move(tempFilePath, filePath);
                    await _logService.LogInfoAsync("Dosya başarıyla taşındı", userId);
                }
                catch (Exception ex)
                {
                    await _logService.LogErrorAsync($"Dosya taşıma hatası: {ex.Message}", ex, userId);
                    throw new IOException($"Dosya taşıma hatası: {ex.Message}", ex);
                }

                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    await _logService.LogErrorAsync("Dosya taşındıktan sonra bulunamadı", new FileNotFoundException("Dosya taşındıktan sonra bulunamadı"), userId);
                    throw new FileNotFoundException("Dosya taşındıktan sonra bulunamadı");
                }

                var file = new FileModel
                {
                    Id = fileId,
                    Name = originalFileName,
                    Path = filePath,
                    ContentType = GetContentType(extension),
                    Size = fileInfo.Length,
                    UserId = userId,
                    UploadedAt = DateTime.UtcNow,
                    FolderId = folderId
                };

                await _logService.LogInfoAsync($"Dosya modeli oluşturuldu. ID: {fileId}, Boyut: {fileInfo.Length}, Klasör ID: {folderId}", userId);

                try
                {
                    _context.Files.Add(file);
                    await _context.SaveChangesAsync();
                    await _logService.LogInfoAsync("Dosya veritabanına kaydedildi", userId);
                }
                catch (Exception ex)
                {
                    await _logService.LogErrorAsync($"Veritabanı kayıt hatası: {ex.Message}", ex, userId);
                    // Dosyayı sil
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    throw new DbUpdateException($"Veritabanı kayıt hatası: {ex.Message}", ex);
                }

                // Clear the cache for this user's file list in the specific folder or root
                _cacheService.Remove($"user_files_and_folders_{userId}_{folderId ?? "root"}");
                _cacheService.Remove($"user_files_{userId}"); // Keep this for backward compatibility if needed elsewhere
                await _logService.LogInfoAsync("Dosya yükleme sonrası ilgili önbellekler temizlendi", userId);

                return fileId;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Dosya yükleme hatası: {ex.Message}", ex, userId);
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

        public async Task<IEnumerable<FileDto>> ListFilesAsync(string userId, string? parentFolderId = null)
        {
            await _logService.LogInfoAsync($"ListFilesAsync başlatıldı. Kullanıcı: {userId}, Klasör ID: {parentFolderId ?? "Root"}", userId);
            try
            {
                // Önbellekten kontrol et
                var cacheKey = $"user_files_and_folders_{userId}_{parentFolderId ?? "root"}";
                var cachedItems = _cacheService.Get<IEnumerable<object>>(cacheKey);
                if (cachedItems != null)
                {
                    await _logService.LogInfoAsync($"Önbellekten dosya/klasör listesi alındı. Kullanıcı: {userId}, Klasör ID: {parentFolderId ?? "Root"}", userId);
                    return (IEnumerable<FileDto>)cachedItems; // DTO yapısı değişeceği için dönüş tipi object olarak düzenlenecek
                }

                await _logService.LogInfoAsync($"Veritabanından dosyalar ve klasörler alınıyor. Kullanıcı: {userId}, Klasör ID: {parentFolderId ?? "Root"}", userId);

                try
                {
                    // Klasördeki dosyaları al (kullanıcının kendi yükledikleri veya kendisiyle paylaşılanlar)
                    var filesInFolder = await _context.Files
                        .Include(f => f.UploadedBy)
                        .Where(f => f.FolderId == parentFolderId && 
                                    (f.UserId == userId || _context.SharedFiles.Any(sf => sf.FileId == f.Id && sf.SharedWithUserId == userId && sf.IsActive)))
                        .ToListAsync();

                    await _logService.LogInfoAsync($"Klasördeki dosyalar alındı. Sayı: {filesInFolder.Count}", userId);

                    // Klasördeki alt klasörleri al
                    var subFolders = await _context.Folders
                        .Where(f => f.ParentFolderId == parentFolderId && f.UserId == userId)
                        .ToListAsync();

                    await _logService.LogInfoAsync($"Klasördeki alt klasörler alındı. Sayı: {subFolders.Count}", userId);

                    var fileDtos = filesInFolder.Select(file => new FileDto
                    {
                        Id = file.Id,
                        Name = file.Name.TrimStart().TrimEnd(),
                        Size = file.Size,
                        UploadedAt = file.UploadedAt,
                        UploadedBy = file.UploadedBy?.Username ?? "Bilinmeyen Kullanıcı",
                        UserId = file.UserId,
                        ContentType = file.ContentType,
                        Icon = GetFileIcon(file.ContentType),
                        FileType = GetFileType(file.ContentType),
                        IsPreviewable = IsPreviewable(file.ContentType)
                    }).ToList();

                    var folderDtos = subFolders.Select(folder => new FileDto // Klasörleri de FileDto gibi gösterelim
                    {
                        Id = folder.Id,
                        Name = folder.Name,
                        Size = 0, // Klasörlerin boyutu 0 olarak gösterilebilir
                        UploadedAt = folder.CreatedAt,
                        UploadedBy = folder.Owner?.Username ?? "Bilinmeyen Kullanıcı",
                        UserId = folder.UserId,
                        ContentType = "folder", // Klasör tipi belirtmek için
                        Icon = "📁", // Klasör ikonu
                        FileType = "Klasör",
                        IsPreviewable = false,
                        FolderId = folder.ParentFolderId // Add ParentFolderId here (Mapping to FolderId in FileDto)
                    }).ToList();

                    // Dosya ve klasör listelerini birleştir ve sırala
                    var combinedList = folderDtos.Concat(fileDtos)
                        .OrderByDescending(item => item.UploadedAt) // Tarihe göre tersten sırala (en yeni üste)
                        .ToList();

                    await _logService.LogInfoAsync($"Birleştirilmiş dosya/klasör sayısı: {combinedList.Count}", userId);

                    // Önbelleğe kaydet
                    _cacheService.Set(cacheKey, combinedList, TimeSpan.FromMinutes(1));

                    return combinedList;
                }
                catch (Exception ex)
                {
                    await _logService.LogErrorAsync($"Veritabanı işlemleri sırasında hata: {ex.Message}", ex, userId);
                    throw;
                }
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Dosya ve klasör listesi alınırken hata: {ex.Message}", ex, userId);
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

        private bool IsPreviewable(string contentType)
        {
            return contentType.ToLower() switch
            {
                var t when t.StartsWith("image/") => true,
                var t when t.StartsWith("video/") => true,
                var t when t.StartsWith("audio/") => true,
                _ => false
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