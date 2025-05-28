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
                    // Dosyayı kopyala ve sonra orijinali sil
                    System.IO.File.Copy(tempFilePath, filePath, true);
                    System.IO.File.Delete(tempFilePath);
                    await _logService.LogInfoAsync("Dosya başarıyla kopyalandı ve geçici dosya silindi", userId);
                }
                catch (Exception ex)
                {
                    await _logService.LogErrorAsync($"Dosya kopyalama hatası: {ex.Message}", ex, userId);
                    throw new IOException($"Dosya kopyalama hatası: {ex.Message}", ex);
                }

                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    await _logService.LogErrorAsync("Dosya kopyalandıktan sonra bulunamadı", new FileNotFoundException("Dosya kopyalandıktan sonra bulunamadı"), userId);
                    throw new FileNotFoundException("Dosya kopyalandıktan sonra bulunamadı");
                }

                await _logService.LogInfoAsync($"Dosya bilgileri - Boyut: {fileInfo.Length}, Oluşturulma: {fileInfo.CreationTime}, Son Değişiklik: {fileInfo.LastWriteTime}", userId);

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

        public async Task<IEnumerable<FileDto>> ListFilesAsync(string userId, string? folderId = null)
        {
            _logService.LogInformation($"[{userId}] ListFilesAsync başlatıldı. Kullanıcı: {userId}, Klasör ID: {folderId ?? "Root"}");

            // Always fetch from database first
            _logService.LogInformation($"[{userId}] Veritabanından dosyalar ve klasörler alınıyor. Kullanıcı: {userId}, Klasör ID: {folderId ?? "Root"}");
            var files = await _context.Files
                .Include(f => f.UploadedBy)
                .Where(f => f.FolderId == (folderId == "Root" ? null : folderId) &&
                           (f.UserId == userId || _context.SharedFiles
                               .Any(s => s.FileId == f.Id && s.SharedWithUserId == userId && s.IsActive)))
                .ToListAsync();

            _logService.LogInformation($"[{userId}] Klasördeki dosyalar alındı. Sayı: {files.Count}");

            var folders = await _context.Folders
                .Where(f => f.ParentFolderId == (folderId == "Root" ? null : folderId) && f.UserId == userId)
                .ToListAsync();

            _logService.LogInformation($"[{userId}] Klasördeki alt klasörler alındı. Sayı: {folders.Count}");

            // Check if physical files exist for database records
            var existingFilesOnDisk = files.Where(f =>
            {
                var filePath = Path.Combine(_uploadPath, f.Id + Path.GetExtension(f.Name));
                bool exists = System.IO.File.Exists(filePath);
                if (!exists)
                {
                    // Log that an orphaned file record was found
                    _logService.LogWarning($"[{userId}] Dikkat: Veritabanında kaydı olan dosya diskte bulunamadı. ID: {f.Id}, Ad: {f.Name}, Path: {filePath}");
                    // Optionally, you could add logic here to delete the orphaned database record
                    // _context.Files.Remove(f); // Uncomment to automatically clean up orphaned records
                }
                return exists;
            }).ToList(); // ToList() ekleyerek filtreleme uygulandı

            _logService.LogInformation($"[{userId}] Disk üzerinde bulunan dosyaların sayısı (veritabanından çekilen ve filtrelenen): {existingFilesOnDisk.Count}"); // Log ekledim

            var result = existingFilesOnDisk.Select(f => new FileDto
            {
                Id = f.Id,
                Name = f.Name.TrimStart().TrimEnd(),
                UserId = f.UserId,
                Size = f.Size,
                UploadedAt = f.UploadedAt,
                ContentType = f.ContentType,
                UploadedBy = f.UploadedBy?.Username ?? "Bilinmeyen Kullanıcı",
                Icon = GetFileIcon(f.ContentType),
                FileType = GetFileType(f.ContentType),
                IsPreviewable = IsPreviewable(f.ContentType),
                FolderId = f.FolderId
            }).Concat(folders.Select(f => new FileDto
            {
                Id = f.Id,
                Name = f.Name.TrimStart().TrimEnd(),
                UserId = f.UserId,
                Size = 0,
                ContentType = "folder",
                UploadedAt = f.CreatedAt,
                UploadedBy = f.Owner?.Username ?? "Bilinmeyen Kullanıcı",
                Icon = "📁",
                FileType = "Klasör",
                IsPreviewable = false,
                FolderId = f.ParentFolderId // Klasörler için ParentFolderId
            })).ToList(); // ToList() ekleyerek sonuç somutlaştırıldı

            _logService.LogInformation($"[{userId}] ListFilesAsync sonuç listesi hazırlanıyor. Toplam öğe: {result.Count}");
            foreach (var item in result)
            {
                _logService.LogInformation($"[{userId}] Sonuç öğesi - ID: {item.Id}, Ad: {item.Name}, Tür: {item.ContentType}");
            }

            return result;
        }

        public async Task<IEnumerable<FileDto>> ListSharedFilesAsync(string userId)
        {
            await _logService.LogInfoAsync($"ListSharedFilesAsync başlatıldı. Kullanıcı: {userId}", userId);
            try
            {
                var sharedFiles = await _context.SharedFiles
                    .Include(sf => sf.File)
                        .ThenInclude(f => f.UploadedBy) // Dosya yükleyen kullanıcıyı include et
                    .Where(sf => sf.SharedWithUserId == userId && sf.IsActive)
                    .Select(sf => sf.File) // Paylaşılan dosyaları seç
                    .Where(f => f != null) // Null olanları filtrele (olmamalı ama önlem)
                    .ToListAsync();

                await _logService.LogInfoAsync($"Kullanıcı {userId} ile paylaşılan dosya sayısı: {sharedFiles.Count}", userId);

                var fileDtos = sharedFiles.Select(file => new FileDto
                {
                    Id = file.Id,
                    Name = file.Name.TrimStart().TrimEnd(),
                    Size = file.Size,
                    UploadedAt = file.UploadedAt,
                    UploadedBy = file.UploadedBy?.Username ?? "Bilinmeyen Kullanıcı",
                    UserId = file.UserId,
                    ContentType = file.ContentType,
                    Icon = GetFileIcon(file.ContentType), // FileService'deki GetFileIcon metodunu kullan
                    FileType = GetFileType(file.ContentType), // FileService'deki GetFileType metodunu kullan
                    IsPreviewable = IsPreviewable(file.ContentType),
                    FolderId = file.FolderId
                }).ToList();

                return fileDtos;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Kullanıcı {userId} ile paylaşılan dosyalar listelenirken hata: {ex.Message}", ex, userId);
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