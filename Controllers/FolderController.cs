using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareVault.API.Data;
using ShareVault.API.Models;
using System.Security.Claims;
using ShareVault.API.Interfaces;
using System.ComponentModel.DataAnnotations;
using ShareVault.API.DTOs;

namespace ShareVault.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class FolderController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IFolderService _folderService;
        private readonly ILogService _logService;
        private readonly IVersioningService _versioningService;

        public FolderController(AppDbContext context, IFolderService folderService, ILogService logService, IVersioningService versioningService)
        {
            _context = context;
            _folderService = folderService;
            _logService = logService;
            _versioningService = versioningService;
        }

        /// <summary>
        /// Yeni bir klasör oluşturur.
        /// </summary>
        /// <param name="request">Klasör oluşturma isteği</param>
        /// <returns>Oluşturulan klasör bilgileri</returns>
        [HttpPost("create")]
        [ProducesResponseType(typeof(FolderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var folder = await _folderService.CreateFolderAsync(request.Name, request.ParentFolderId, userId);
                return Ok(folder);
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Klasör oluşturma hatası", ex, userId);
                return StatusCode(500, "Klasör oluşturulurken bir hata oluştu");
            }
        }

        /// <summary>
        /// Kullanıcının klasörlerini listeler.
        /// </summary>
        /// <param name="parentFolderId">Üst klasör ID (opsiyonel)</param>
        /// <returns>Klasör listesi</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<FolderDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ListFolders([FromQuery] string? parentFolderId = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var folders = await _folderService.ListFoldersAsync(userId, parentFolderId);
                return Ok(folders);
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Klasör listeleme hatası", ex, userId);
                return StatusCode(500, "Klasörler listelenirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Klasör adını günceller.
        /// </summary>
        /// <param name="folderId">Klasör ID</param>
        /// <param name="name">Yeni klasör adı</param>
        /// <returns>Güncellenen klasör bilgileri</returns>
        [HttpPut("{folderId}")]
        [ProducesResponseType(typeof(FolderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateFolder(string folderId, [FromQuery] string name)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                if (string.IsNullOrEmpty(name))
                    return BadRequest("Klasör adı boş olamaz");

                var folder = await _folderService.UpdateFolderAsync(folderId, name, userId);
                return Ok(folder);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Klasör güncelleme hatası", ex, userId);
                return StatusCode(500, "Klasör güncellenirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Klasörü siler.
        /// </summary>
        /// <param name="folderId">Klasör ID</param>
        /// <returns>İşlem sonucu</returns>
        [HttpDelete("delete/{folderId}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteFolder(string folderId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                await _folderService.DeleteFolderAsync(folderId, userId);
                return Ok("Klasör başarıyla silindi");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Klasör silme hatası", ex, userId);
                return StatusCode(500, "Klasör silinirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Klasörü başka bir klasöre taşır.
        /// </summary>
        /// <param name="folderId">Taşınacak klasör ID</param>
        /// <param name="newParentFolderId">Hedef klasör ID (opsiyonel)</param>
        /// <returns>İşlem sonucu</returns>
        [HttpPut("{folderId}/move")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MoveFolder(string folderId, [FromQuery] string? newParentFolderId = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                await _folderService.MoveFolderAsync(folderId, newParentFolderId, userId);
                return Ok("Klasör başarıyla taşındı");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Klasör taşıma hatası", ex, userId);
                return StatusCode(500, "Klasör taşınırken bir hata oluştu");
            }
        }

        /// <summary>
        /// Klasör içeriğini listeler.
        /// </summary>
        [HttpGet("{folderId}/contents")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFolderContents(string folderId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var folder = await _context.Folders
                    .Include(f => f.SubFolders)
                    .Include(f => f.Files)
                    .FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == userId);

                if (folder == null)
                    return NotFound("Klasör bulunamadı");

                var subFolders = folder.SubFolders.Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.CreatedAt,
                    f.UpdatedAt,
                    Type = "folder"
                });

                var files = folder.Files.Select(f => new
                {
                    f.Id,
                    Name = f.Name.TrimStart().TrimEnd(),
                    f.ContentType,
                    f.Size,
                    f.UploadedAt,
                    Type = "file",
                    Icon = GetFileIcon(f.ContentType),
                    FileType = GetFileType(f.ContentType),
                    IsPreviewable = IsPreviewable(f.ContentType)
                });

                return Ok(new
                {
                    Folder = new
                    {
                        folder.Id,
                        folder.Name,
                        folder.ParentFolderId,
                        folder.CreatedAt,
                        folder.UpdatedAt
                    },
                    Contents = new
                    {
                        Folders = subFolders,
                        Files = files
                    }
                });
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Klasör içeriği listeleme hatası", ex, userId);
                return StatusCode(500, "Klasör içeriği listelenirken bir hata oluştu");
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

        private bool IsPreviewable(string contentType)
        {
            return contentType.ToLower() switch
            {
                var t when t.StartsWith("image/") => true,
                var t when t.Contains("pdf") => true,
                _ => false
            };
        }

        #region Klasör Paylaşımı Endpoint'leri

        /// <summary>
        /// Klasörü başka bir kullanıcı ile paylaşır
        /// </summary>
        /// <param name="folderId">Paylaşılacak klasör ID</param>
        /// <param name="request">Paylaşım bilgileri</param>
        /// <returns>Paylaşım bilgileri</returns>
        [HttpPost("{folderId}/share")]
        [ProducesResponseType(typeof(SharedFolderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ShareFolder(string folderId, [FromBody] ShareFolderRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                if (string.IsNullOrEmpty(request.SharedWithUserId))
                    return BadRequest("Paylaşım yapılacak kullanıcı belirtilmelidir");

                var sharedFolder = await _folderService.ShareFolderAsync(
                    folderId, 
                    userId, 
                    request.SharedWithUserId, 
                    request.Permission, 
                    request.ExpiresAt, 
                    request.ShareNote ?? string.Empty);

                return Ok(sharedFolder);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Klasör paylaşım hatası", ex, userId);
                return StatusCode(500, "Klasör paylaşılırken bir hata oluştu");
            }
        }

        /// <summary>
        /// Kullanıcı ile paylaşılan klasörleri listeler
        /// </summary>
        /// <returns>Paylaşılan klasör listesi</returns>
        [HttpGet("shared-with-me")]
        [ProducesResponseType(typeof(IEnumerable<SharedFolderDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetSharedFolders()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var sharedFolders = await _folderService.GetSharedFoldersAsync(userId);
                return Ok(sharedFolders);
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Paylaşılan klasörleri listeleme hatası", ex, userId);
                return StatusCode(500, "Paylaşılan klasörler listelenirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Klasörün paylaşıldığı kullanıcıları listeler
        /// </summary>
        /// <param name="folderId">Klasör ID</param>
        /// <returns>Klasör paylaşım listesi</returns>
        [HttpGet("{folderId}/shared-users")]
        [ProducesResponseType(typeof(IEnumerable<SharedFolderDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetFolderShares(string folderId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var folderShares = await _folderService.GetFolderSharesAsync(folderId, userId);
                return Ok(folderShares);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Klasör paylaşımlarını listeleme hatası", ex, userId);
                return StatusCode(500, "Klasör paylaşımları listelenirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Klasör paylaşımını iptal eder
        /// </summary>
        /// <param name="folderId">Klasör ID</param>
        /// <param name="request">Erişimi iptal edilecek kullanıcı bilgileri</param>
        /// <returns>İşlem sonucu</returns>
        [HttpPost("{folderId}/revoke-access")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RevokeFolderAccess(string folderId, [FromBody] RevokeFolderAccessRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                if (string.IsNullOrEmpty(request.SharedWithUserId))
                    return BadRequest("Paylaşımı iptal edilecek kullanıcı belirtilmelidir");

                await _folderService.RevokeFolderAccessAsync(folderId, request.SharedWithUserId, userId);
                return Ok("Klasör erişimi başarıyla iptal edildi");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Klasör erişimi iptal hatasi", ex, userId);
                return StatusCode(500, "Klasör erişimi iptal edilirken bir hata oluştu");
            }
        }

        #endregion

        /// <summary>
        /// Klasörün versiyonlarını listeler.
        /// </summary>
        /// <param name="folderId">Klasör ID</param>
        /// <returns>Klasör versiyonları listesi</returns>
        [HttpGet("{folderId}/versions")]
        [ProducesResponseType(typeof(IEnumerable<FolderVersion>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFolderVersions(string folderId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var folder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == folderId);
                if (folder == null)
                    return NotFound("Klasör bulunamadı");

                var versions = await _versioningService.GetFolderVersionsAsync(folderId);
                return Ok(versions);
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Klasör versiyonları listelenirken hata oluştu", ex, userId);
                return StatusCode(500, "Klasör versiyonları listelenirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Belirli bir klasör versiyonunu görüntüler.
        /// </summary>
        /// <param name="folderId">Klasör ID</param>
        /// <param name="versionNumber">Versiyon numarası</param>
        /// <returns>Klasör yapısı</returns>
        [HttpGet("{folderId}/versions/{versionNumber}")]
        [ProducesResponseType(typeof(FolderVersion), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFolderVersion(string folderId, string versionNumber)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var version = await _versioningService.GetFolderVersionAsync(folderId, versionNumber);
                if (version == null)
                    return NotFound("Klasör versiyonu bulunamadı");

                return Ok(version);
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Klasör versiyonu görüntülenirken hata oluştu", ex, userId);
                return StatusCode(500, "Klasör versiyonu görüntülenirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Yeni bir klasör versiyonu oluşturur.
        /// </summary>
        /// <param name="folderId">Klasör ID</param>
        /// <param name="changeNotes">Değişiklik notları</param>
        /// <returns>Oluşturulan versiyon bilgisi</returns>
        [HttpPost("{folderId}/versions")]
        [ProducesResponseType(typeof(FolderVersion), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateFolderVersion(string folderId, [FromBody] string? changeNotes = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var folder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == folderId);
                if (folder == null)
                    return NotFound("Klasör bulunamadı");

                var version = await _versioningService.CreateFolderVersionAsync(folder, userId, changeNotes);
                return Ok(version);
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync("Klasör versiyonu oluşturulurken hata oluştu", ex, userId);
                return StatusCode(500, "Klasör versiyonu oluşturulurken bir hata oluştu");
            }
        }
    }
} 