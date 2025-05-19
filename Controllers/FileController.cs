using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareVault.API.Data;
using ShareVault.API.Models;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using ShareVault.API.Interfaces;

namespace ShareVault.API.Controllers
{
    /// <summary>
    /// Dosya yükleme, indirme ve paylaşım işlemlerini yönetir.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class FileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private const long MaxFileSize = 100 * 1024 * 1024; // 100MB
        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png", ".gif" };
        private readonly ShareVault.API.Interfaces.IFileService _fileService;
        private readonly ILogService _logService;
        private readonly string _tempUploadPath;

        public FileController(AppDbContext context, IWebHostEnvironment environment, ShareVault.API.Interfaces.IFileService fileService, ILogService logService)
        {
            _context = context;
            _environment = environment;
            _fileService = fileService;
            _logService = logService;
            _tempUploadPath = Path.Combine(_environment.ContentRootPath, "TempUploads");
            
            if (!Directory.Exists(_tempUploadPath))
            {
                Directory.CreateDirectory(_tempUploadPath);
            }
        }

        /// <summary>
        /// Yeni bir dosyayı geçici olarak yükler.
        /// </summary>
        /// <param name="file">Yüklenecek dosya</param>
        /// <returns>Yüklenen dosyanın geçici adı ve orijinal adı</returns>
        /// <response code="200">Dosya başarıyla yüklendi</response>
        /// <response code="400">Geçersiz dosya boyutu veya türü</response>
        /// <response code="401">Yetkilendirme hatası</response>
        [HttpPost("upload-temp")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UploadTempFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("Dosya seçilmedi");

                if (file.Length > MaxFileSize)
                    return BadRequest($"Dosya boyutu {MaxFileSize / (1024 * 1024)}MB'dan büyük olamaz");

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                    return BadRequest("Geçersiz dosya türü");

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var tempFileName = $"{Guid.NewGuid()}{extension}";
                var tempFilePath = Path.Combine(_tempUploadPath, tempFileName);

                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new { tempFileName, originalName = file.FileName });
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Geçici dosya yükleme hatası", ex, userId);
                return StatusCode(500, "Dosya yüklenirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Geçici olarak yüklenen dosyayı kalıcı hale getirir.
        /// </summary>
        /// <param name="request">Tamamlama isteği</param>
        /// <returns>Yüklenen dosyanın ID</returns>
        /// <response code="200">Dosya başarıyla yüklendi</response>
        /// <response code="404">Geçici dosya bulunamadı</response>
        [HttpPost("complete-upload")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CompleteUpload([FromBody] CompleteUploadRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var tempFilePath = Path.Combine(_tempUploadPath, request.TempFileName);
                if (!System.IO.File.Exists(tempFilePath))
                    return NotFound("Geçici dosya bulunamadı");

                var fileId = await _fileService.CompleteUploadAsync(tempFilePath, request.OriginalFileName, userId);
                return Ok(new { fileId });
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Dosya yükleme tamamlama hatası", ex, userId);
                return StatusCode(500, "Dosya yüklenirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Geçici olarak yüklenen dosyayı siler.
        /// </summary>
        /// <param name="request">İptal isteği</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Geçici dosya başarıyla silindi</response>
        /// <response code="404">Geçici dosya bulunamadı</response>
        [HttpPost("cancel-upload")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public IActionResult CancelUpload([FromBody] CancelUploadRequest request)
        {
            try
            {
                var tempFilePath = Path.Combine(_tempUploadPath, request.TempFileName);
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
                return Ok("Geçici dosya başarıyla silindi");
            }
            catch
            {
                return StatusCode(500, "Geçici dosya silinirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Belirtilen ID'ye sahip dosyayı indirir.
        /// </summary>
        /// <param name="fileId">Dosya ID</param>
        /// <returns>Dosya içeriği</returns>
        /// <response code="200">Dosya başarıyla indirildi</response>
        /// <response code="404">Dosya bulunamadı</response>
        /// <response code="403">Dosyaya erişim izni yok</response>
        [HttpGet("download/{fileId}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DownloadFile(string fileId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();
            
                var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == fileId);
                if (file == null)
                    return NotFound("Dosya bulunamadı");

                var fileBytes = await _fileService.DownloadFileAsync(fileId, userId);
                return File(fileBytes, "application/octet-stream", file.Name);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Dosya bulunamadı");
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Dosya indirme hatası", ex, userId);
                return StatusCode(500, "Dosya indirilirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Kullanıcının dosyalarını ve kendisiyle paylaşılan dosyaları listeler.
        /// </summary>
        /// <returns>Kullanıcının dosyaları ve paylaşılan dosyalar</returns>
        /// <response code="200">Dosya listesi başarıyla getirildi</response>
        [HttpGet("list")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListFiles()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var files = await _fileService.ListFilesAsync(userId);
                var fileDtos = files.Select(f => new
                {
                    f.Id,
                    Name = f.Name.TrimStart().TrimEnd(),
                    f.ContentType,
                    f.Size,
                    f.UploadedAt,
                    UploadedBy = f.UserId == userId ? "Siz" : f.UploadedBy,
                    f.UserId,
                    Icon = GetFileIcon(f.ContentType),
                    FileType = GetFileType(f.ContentType)
                });

                return Ok(fileDtos);
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Dosya listeleme hatası", ex, userId);
                return StatusCode(500, "Dosyalar listelenirken bir hata oluştu");
            }
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

        /// <summary>
        /// Bir dosyayı birden fazla kullanıcıyla paylaşır.
        /// </summary>
        /// <param name="request">Paylaşım isteği</param>
        /// <returns>Paylaşım sonuçları</returns>
        /// <response code="200">Paylaşım işlemi tamamlandı</response>
        /// <response code="400">Geçersiz paylaşım isteği</response>
        /// <response code="404">Dosya bulunamadı</response>
        [HttpPost("share-multiple")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ShareWithMultipleUsers([FromBody] ShareMultipleRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var file = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == request.FileId && f.UserId == userId);

            if (file == null)
                return NotFound("Dosya bulunamadı veya bu dosyayı paylaşma yetkiniz yok.");

            if (request.UserIds.Contains(userId))
                return BadRequest("Kendinizle dosya paylaşamazsınız.");

            // Kullanıcı ID'lerinin geçerliliğini kontrol et
            var validUsers = await _context.Users
                .Where(u => request.UserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Username })
                .ToListAsync();

            if (!validUsers.Any())
                return BadRequest("Geçerli kullanıcı bulunamadı.");

            var results = new List<ShareResult>();
            var existingShares = await _context.SharedFiles
                .Where(sf => sf.FileId == request.FileId && request.UserIds.Contains(sf.SharedWithUserId) && sf.IsActive)
                .Select(sf => sf.SharedWithUserId)
                .ToListAsync();

            var newShares = new List<SharedFile>();
            foreach (var user in validUsers)
            {
                if (user.Id == userId)
                    continue;

                if (existingShares.Contains(user.Id))
                {
                    results.Add(new ShareResult
                    {
                        UserId = user.Id,
                        Success = false,
                        Message = "Dosya zaten bu kullanıcıyla paylaşılmış"
                    });
                    continue;
                }

                newShares.Add(new SharedFile
                {
                    Id = Guid.NewGuid().ToString(),
                    FileId = request.FileId,
                    SharedByUserId = userId,
                    SharedWithUserId = user.Id,
                    SharedAt = DateTime.UtcNow,
                    IsActive = true,
                    File = file!,
                    SharedByUser = await _context.Users.FindAsync(userId) ?? throw new InvalidOperationException("Paylaşan kullanıcı bulunamadı"),
                    SharedWithUser = await _context.Users.FindAsync(user.Id) ?? throw new InvalidOperationException("Paylaşılan kullanıcı bulunamadı")
                });

                results.Add(new ShareResult
                {
                    UserId = user.Id,
                    Success = true,
                    Message = "Dosya başarıyla paylaşıldı"
                });
            }

            if (newShares.Any())
            {
                await _context.SharedFiles.AddRangeAsync(newShares);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                FileId = request.FileId,
                FileName = file.Name,
                Results = results
            });
        }

        /// <summary>
        /// Bir dosyayı siler.
        /// </summary>
        /// <param name="fileId">Silinecek dosya ID</param>
        /// <returns>Silme işlemi sonucu</returns>
        /// <response code="200">Dosya başarıyla silindi</response>
        /// <response code="404">Dosya bulunamadı</response>
        [HttpDelete("{fileId}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteFile(string fileId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                await _fileService.DeleteFileAsync(fileId, userId);
                return Ok();
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Dosya bulunamadı");
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Dosya silme hatası", ex, userId);
                return StatusCode(500, "Dosya silinirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Bir dosyanın paylaşım erişimini kaldırır.
        /// </summary>
        /// <param name="fileId">Dosya ID</param>
        /// <param name="sharedWithUserId">Erişimi kaldırılacak kullanıcı ID</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Erişim başarıyla kaldırıldı</response>
        /// <response code="404">Dosya veya paylaşım bulunamadı</response>
        [HttpPost("revoke-access")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevokeAccess([Required] string fileId, [Required] string sharedWithUserId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var file = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);

            if (file == null)
                return NotFound("Dosya bulunamadı veya bu işlem için yetkiniz yok.");

            var sharedFile = await _context.SharedFiles
                .FirstOrDefaultAsync(sf => sf.FileId == fileId && sf.SharedWithUserId == sharedWithUserId && sf.IsActive);

            if (sharedFile == null)
                return NotFound("Paylaşım bulunamadı.");

            sharedFile.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok("Dosya erişimi başarıyla kaldırıldı.");
        }

        /// <summary>
        /// Bir dosyanın kimlerle paylaşıldığını listeler.
        /// </summary>
        /// <param name="fileId">Dosya ID</param>
        /// <returns>Paylaşım listesi</returns>
        /// <response code="200">Paylaşım listesi başarıyla getirildi</response>
        /// <response code="404">Dosya bulunamadı</response>
        /// <response code="403">Dosyaya erişim izni yok</response>
        [HttpGet("shared-users/{fileId}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetSharedUsers([Required] string fileId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var file = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (file == null)
                return NotFound("Dosya bulunamadı.");

            if (file.UserId != userId && userRole != "Admin")
                return Forbid();

            var sharedUsers = await _context.SharedFiles
                .Where(sf => sf.FileId == fileId && sf.IsActive)
                .Select(sf => new
                {
                    UserId = sf.SharedWithUserId,
                    Username = sf.SharedWithUser.Username,
                    Email = sf.SharedWithUser.Email,
                    SharedAt = sf.SharedAt
                })
                .ToListAsync();

            return Ok(new
            {
                FileId = fileId,
                FileName = file.Name,
                SharedUsers = sharedUsers
            });
        }

        /// <summary>
        /// Bir dosyanın paylaşım tarihini günceller.
        /// </summary>
        /// <param name="fileId">Dosya ID</param>
        /// <param name="sharedWithUserId">Paylaşılan kullanıcı ID</param>
        /// <returns>Güncelleme sonucu</returns>
        /// <response code="200">Paylaşım tarihi başarıyla güncellendi</response>
        /// <response code="404">Dosya veya paylaşım bulunamadı</response>
        /// <response code="403">Bu işlem için yetkiniz yok</response>
        [HttpPut("update-share-date")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateShareDate([Required] string fileId, [Required] string sharedWithUserId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var file = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (file == null)
                return NotFound("Dosya bulunamadı.");

            if (file.UserId != userId && userRole != "Admin")
                return Forbid();

            var sharedFile = await _context.SharedFiles
                .FirstOrDefaultAsync(sf => sf.FileId == fileId && sf.SharedWithUserId == sharedWithUserId && sf.IsActive);

            if (sharedFile == null)
                return NotFound("Aktif paylaşım bulunamadı.");

            sharedFile.SharedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Paylaşım tarihi güncellendi",
                FileId = fileId,
                SharedWithUserId = sharedWithUserId,
                NewShareDate = sharedFile.SharedAt
            });
        }

        /// <summary>
        /// Admin için tüm dosyaların paylaşım geçmişini listeler.
        /// </summary>
        /// <returns>Paylaşım geçmişi</returns>
        /// <response code="200">Paylaşım geçmişi başarıyla getirildi</response>
        /// <response code="403">Bu işlem için yetkiniz yok</response>
        [HttpGet("admin/share-history")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetShareHistory()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
                return Forbid();

            var shareHistory = await _context.SharedFiles
                .Include(sf => sf.File)
                .Include(sf => sf.SharedWithUser)
                .Include(sf => sf.SharedByUser)
                .OrderByDescending(sf => sf.SharedAt)
                .Select(sf => new
                {
                    FileId = sf.FileId,
                    FileName = sf.File.Name,
                    FileOwner = sf.SharedByUser.Username,
                    SharedWithUser = new
                    {
                        Id = sf.SharedWithUser.Id,
                        Username = sf.SharedWithUser.Username,
                        Email = sf.SharedWithUser.Email
                    },
                    SharedAt = sf.SharedAt,
                    IsActive = sf.IsActive
                })
                .ToListAsync();

            return Ok(new
            {
                TotalShares = shareHistory.Count,
                ActiveShares = shareHistory.Count(s => s.IsActive),
                InactiveShares = shareHistory.Count(s => !s.IsActive),
                ShareHistory = shareHistory
            });
        }

        /// <summary>
        /// Bir dosyayı yayınlar.
        /// </summary>
        /// <param name="fileId">Yayınlanacak dosya ID</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Dosya başarıyla yayınlandı</response>
        /// <response code="404">Dosya bulunamadı</response>
        [HttpPost("publish/{fileId}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PublishFile(string fileId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);
                if (file == null)
                    return NotFound("Dosya bulunamadı");

                file.IsPublic = true;
                await _context.SaveChangesAsync();

                return Ok("Dosya başarıyla yayınlandı");
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Dosya yayınlama hatası", ex, userId);
                return StatusCode(500, "Dosya yayınlanırken bir hata oluştu");
            }
        }
    }

    public class ShareMultipleRequest
    {
        [Required]
        public required string FileId { get; set; }

        [Required]
        public required List<string> UserIds { get; set; }
    }

    public class ShareResult
    {
        public required string UserId { get; set; }
        public bool Success { get; set; }
        public required string Message { get; set; }
    }

    public class CompleteUploadRequest
    {
        [Required]
        public required string TempFileName { get; set; }

        [Required]
        public required string OriginalFileName { get; set; }
    }

    public class CancelUploadRequest
    {
        [Required]
        public required string TempFileName { get; set; }
    }
} 