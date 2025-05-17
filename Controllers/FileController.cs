using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareVault.API.Data;
using ShareVault.API.Models;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

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

        public FileController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        /// <summary>
        /// Yeni bir dosya yükler.
        /// </summary>
        /// <param name="file">Yüklenecek dosya</param>
        /// <returns>Yüklenen dosyanın ID ve adı</returns>
        /// <response code="200">Dosya başarıyla yüklendi</response>
        /// <response code="400">Geçersiz dosya boyutu veya türü</response>
        /// <response code="401">Yetkilendirme hatası</response>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UploadFile([Required] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Dosya seçilmedi.");

            if (file.Length > MaxFileSize)
                return BadRequest($"Dosya boyutu {MaxFileSize / (1024 * 1024)}MB'dan büyük olamaz.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return BadRequest("Desteklenmeyen dosya türü.");

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileModel = new FileModel
            {
                FileName = file.FileName,
                FilePath = uniqueFileName,
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadDate = DateTime.UtcNow,
                UserId = userId
            };

            _context.Files.Add(fileModel);
            await _context.SaveChangesAsync();

            return Ok(new { fileModel.Id, fileModel.FileName });
        }

        /// <summary>
        /// Belirtilen ID'ye sahip dosyayı indirir.
        /// </summary>
        /// <param name="id">Dosya ID</param>
        /// <returns>Dosya içeriği</returns>
        /// <response code="200">Dosya başarıyla indirildi</response>
        /// <response code="404">Dosya bulunamadı</response>
        /// <response code="403">Dosyaya erişim izni yok</response>
        [HttpGet("download/{id}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DownloadFile([Required] int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            var file = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == id);

            if (file == null)
                return NotFound("Dosya bulunamadı.");

            if (file.UserId != userId)
            {
                var isShared = await _context.SharedFiles
                    .AnyAsync(sf => sf.FileId == id && sf.SharedWithUserId == userId && sf.IsActive);

                if (!isShared)
                    return Forbid();
            }

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", file.FilePath);
            
            if (!System.IO.File.Exists(filePath))
                return NotFound("Dosya fiziksel olarak bulunamadı.");

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, file.ContentType, file.FileName);
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
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<FileModel> filesQuery = _context.Files;

            if (userRole != "Admin")
            {
                filesQuery = filesQuery.Where(f => f.UserId == userId);
            }

            var files = await filesQuery
                .Select(f => new
                {
                    f.Id,
                    f.FileName,
                    f.FileSize,
                    f.UploadDate,
                    f.ContentType,
                    Owner = f.User.FullName
                })
                .ToListAsync();

            var sharedFiles = await _context.SharedFiles
                .Where(sf => sf.SharedWithUserId == userId && sf.IsActive)
                .Select(sf => new
                {
                    sf.File.Id,
                    sf.File.FileName,
                    sf.File.FileSize,
                    sf.File.UploadDate,
                    sf.File.ContentType,
                    SharedBy = sf.File.User.FullName
                })
                .ToListAsync();

            return Ok(new
            {
                MyFiles = files,
                SharedWithMe = sharedFiles
            });
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
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var file = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == request.FileId && f.UserId == userId);

            if (file == null)
                return NotFound("Dosya bulunamadı veya bu dosyayı paylaşma yetkiniz yok.");

            if (request.UserIds.Contains(userId))
                return BadRequest("Kendinizle dosya paylaşamazsınız.");

            // Kullanıcı ID'lerinin geçerliliğini kontrol et
            var validUserIds = await _context.Users
                .Where(u => request.UserIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();

            if (!validUserIds.Any())
                return BadRequest("Geçerli kullanıcı bulunamadı.");

            var results = new List<ShareResult>();
            var existingShares = await _context.SharedFiles
                .Where(sf => sf.FileId == request.FileId && request.UserIds.Contains(sf.SharedWithUserId) && sf.IsActive)
                .Select(sf => sf.SharedWithUserId)
                .ToListAsync();

            foreach (var sharedWithUserId in request.UserIds)
            {
                if (sharedWithUserId == userId)
                    continue;

                if (!validUserIds.Contains(sharedWithUserId))
                {
                    results.Add(new ShareResult
                    {
                        UserId = sharedWithUserId,
                        Success = false,
                        Message = "Kullanıcı bulunamadı"
                    });
                    continue;
                }

                if (existingShares.Contains(sharedWithUserId))
                {
                    results.Add(new ShareResult
                    {
                        UserId = sharedWithUserId,
                        Success = false,
                        Message = "Dosya zaten bu kullanıcıyla paylaşılmış"
                    });
                    continue;
                }

                var sharedFile = new SharedFile
                {
                    FileId = request.FileId,
                    SharedWithUserId = sharedWithUserId,
                    SharedDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.SharedFiles.Add(sharedFile);
                results.Add(new ShareResult
                {
                    UserId = sharedWithUserId,
                    Success = true,
                    Message = "Dosya başarıyla paylaşıldı"
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                FileId = request.FileId,
                FileName = file.FileName,
                Results = results
            });
        }

        /// <summary>
        /// Bir dosyayı siler.
        /// </summary>
        /// <param name="id">Silinecek dosya ID</param>
        /// <returns>Silme işlemi sonucu</returns>
        /// <response code="200">Dosya başarıyla silindi</response>
        /// <response code="404">Dosya bulunamadı</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteFile([Required] int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var file = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == id && (f.UserId == userId || userRole == "Admin"));

            if (file == null)
                return NotFound("Dosya bulunamadı veya silme yetkiniz yok.");

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", file.FilePath);
            
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            var sharedFiles = await _context.SharedFiles
                .Where(sf => sf.FileId == id)
                .ToListAsync();

            _context.SharedFiles.RemoveRange(sharedFiles);
            _context.Files.Remove(file);
            await _context.SaveChangesAsync();

            return Ok("Dosya başarıyla silindi.");
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
        public async Task<IActionResult> RevokeAccess([Required] int fileId, [Required] int sharedWithUserId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

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
        public async Task<IActionResult> GetSharedUsers([Required] int fileId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
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
                    FullName = sf.SharedWithUser.FullName,
                    Email = sf.SharedWithUser.Email,
                    SharedDate = sf.SharedDate
                })
                .ToListAsync();

            return Ok(new
            {
                FileId = fileId,
                FileName = file.FileName,
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
        public async Task<IActionResult> UpdateShareDate([Required] int fileId, [Required] int sharedWithUserId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var file = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (file == null)
                return NotFound("Dosya bulunamadı.");

            // Dosya sahibi veya Admin olmalı
            if (file.UserId != userId && userRole != "Admin")
                return Forbid();

            var sharedFile = await _context.SharedFiles
                .FirstOrDefaultAsync(sf => sf.FileId == fileId && sf.SharedWithUserId == sharedWithUserId && sf.IsActive);

            if (sharedFile == null)
                return NotFound("Aktif paylaşım bulunamadı.");

            sharedFile.SharedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Paylaşım tarihi güncellendi",
                FileId = fileId,
                SharedWithUserId = sharedWithUserId,
                NewShareDate = sharedFile.SharedDate
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
                .Include(sf => sf.File.User)
                .OrderByDescending(sf => sf.SharedDate)
                .Select(sf => new
                {
                    FileId = sf.FileId,
                    FileName = sf.File.FileName,
                    FileOwner = sf.File.User.FullName,
                    SharedWithUser = new
                    {
                        Id = sf.SharedWithUser.Id,
                        FullName = sf.SharedWithUser.FullName,
                        Email = sf.SharedWithUser.Email
                    },
                    SharedDate = sf.SharedDate,
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
    }

    public class ShareMultipleRequest
    {
        [Required]
        public int FileId { get; set; }

        [Required]
        public List<int> UserIds { get; set; }
    }

    public class ShareResult
    {
        public int UserId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
} 