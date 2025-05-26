using Microsoft.EntityFrameworkCore;
using ShareVault.API.Data;
using ShareVault.API.Models;
using ShareVault.API.Interfaces;
using ShareVault.API.DTOs;

namespace ShareVault.API.Services
{
    public class FolderService : IFolderService
    {
        private readonly AppDbContext _context;
        private readonly ILogService _logService;
        private readonly ICacheService _cacheService;

        public FolderService(
            AppDbContext context,
            ILogService logService,
            ICacheService cacheService)
        {
            _context = context;
            _logService = logService;
            _cacheService = cacheService;
        }

        public async Task<FolderDto> CreateFolderAsync(string name, string? parentFolderId, string userId)
        {
            try
            {
                await _logService.LogInfoAsync($"Klasör oluşturma başlatıldı. Ad: {name}, Üst Klasör: {parentFolderId ?? "Root"}, Kullanıcı: {userId}", userId);

                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException("Klasör adı boş olamaz");

                if (parentFolderId != null)
                {
                    var parentFolder = await _context.Folders
                        .FirstOrDefaultAsync(f => f.Id == parentFolderId && f.UserId == userId);

                    if (parentFolder == null)
                        throw new KeyNotFoundException("Üst klasör bulunamadı");
                }

                var folder = new Folder
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    UserId = userId,
                    ParentFolderId = parentFolderId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Folders.Add(folder);
                await _context.SaveChangesAsync();

                // Önbelleği temizle
                _cacheService.Remove($"user_files_and_folders_{userId}_{parentFolderId ?? "root"}");

                await _logService.LogInfoAsync($"Klasör başarıyla oluşturuldu. ID: {folder.Id}", userId);

                return new FolderDto
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    ParentFolderId = folder.ParentFolderId,
                    CreatedAt = folder.CreatedAt,
                    UpdatedAt = folder.UpdatedAt,
                    FileCount = folder.Files.Count,
                    SubFolderCount = folder.SubFolders.Count,
                    UserId = folder.UserId
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Klasör oluşturma hatası: {ex.Message}", ex, userId);
                throw;
            }
        }

        public async Task<IEnumerable<FolderDto>> ListFoldersAsync(string userId, string? parentFolderId = null)
        {
            try
            {
                await _logService.LogInfoAsync($"Klasör listeleme başlatıldı. Kullanıcı: {userId}", userId);

                // Önbellekten kontrol et
                var cacheKey = $"user_folders_{userId}_{parentFolderId}";
                var cachedFolders = _cacheService.Get<List<FolderDto>>(cacheKey);
                if (cachedFolders != null)
                {
                    await _logService.LogInfoAsync($"Önbellekten klasör listesi alındı. Kullanıcı: {userId}", userId);
                    return cachedFolders;
                }

                var folders = await _context.Folders
                    .Where(f => f.UserId == userId && f.ParentFolderId == parentFolderId)
                    .Include(f => f.Files)
                    .Include(f => f.SubFolders)
                    .ToListAsync();

                var folderDtos = folders.Select(f => new FolderDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    ParentFolderId = f.ParentFolderId,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt,
                    FileCount = f.Files.Count,
                    SubFolderCount = f.SubFolders.Count,
                    UserId = f.UserId
                }).ToList();

                // Önbelleğe kaydet
                _cacheService.Set(cacheKey, folderDtos, TimeSpan.FromMinutes(5));

                await _logService.LogInfoAsync($"Klasör listesi başarıyla alındı. Sayı: {folderDtos.Count}", userId);

                return folderDtos;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Klasör listeleme hatası: {ex.Message}", ex, userId);
                throw;
            }
        }

        public async Task<FolderDto> UpdateFolderAsync(string folderId, string name, string userId)
        {
            try
            {
                await _logService.LogInfoAsync($"Klasör güncelleme başlatıldı. ID: {folderId}, Kullanıcı: {userId}", userId);

                var folder = await _context.Folders
                    .FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == userId);

                if (folder == null)
                {
                    await _logService.LogErrorAsync($"Klasör bulunamadı: {folderId}", new KeyNotFoundException("Klasör bulunamadı"), userId);
                    throw new KeyNotFoundException("Klasör bulunamadı");
                }

                folder.Name = name;
                folder.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Önbelleği temizle
                _cacheService.Remove($"user_folders_{userId}");
                _cacheService.Remove($"user_folders_{userId}_{folder.ParentFolderId}");

                await _logService.LogInfoAsync($"Klasör başarıyla güncellendi. ID: {folderId}", userId);

                return new FolderDto
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    ParentFolderId = folder.ParentFolderId,
                    CreatedAt = folder.CreatedAt,
                    UpdatedAt = folder.UpdatedAt,
                    FileCount = folder.Files.Count,
                    SubFolderCount = folder.SubFolders.Count,
                    UserId = folder.UserId
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Klasör güncelleme hatası: {ex.Message}", ex, userId);
                throw;
            }
        }

        public async Task DeleteFolderAsync(string folderId, string userId)
        {
            try
            {
                await _logService.LogInfoAsync($"Klasör silme başlatıldı. ID: {folderId}, Kullanıcı: {userId}", userId);

                var folder = await _context.Folders
                    .Include(f => f.Files)
                    .Include(f => f.SubFolders)
                    .FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == userId);

                if (folder == null)
                {
                    await _logService.LogErrorAsync($"Klasör bulunamadı: {folderId}", new KeyNotFoundException("Klasör bulunamadı"), userId);
                    throw new KeyNotFoundException("Klasör bulunamadı");
                }

                // Alt klasörleri ve dosyaları sil
                foreach (var subFolder in folder.SubFolders)
                {
                    await DeleteFolderAsync(subFolder.Id, userId);
                }

                _context.Folders.Remove(folder);
                await _context.SaveChangesAsync();

                // Önbelleği temizle
                _cacheService.Remove($"user_folders_{userId}");
                _cacheService.Remove($"user_folders_{userId}_{folder.ParentFolderId}");

                await _logService.LogInfoAsync($"Klasör başarıyla silindi. ID: {folderId}", userId);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Klasör silme hatası: {ex.Message}", ex, userId);
                throw;
            }
        }

        public async Task MoveFolderAsync(string folderId, string? newParentFolderId, string userId)
        {
            try
            {
                await _logService.LogInfoAsync($"Klasör taşıma başlatıldı. ID: {folderId}, Kullanıcı: {userId}", userId);

                var folder = await _context.Folders
                    .FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == userId);

                if (folder == null)
                {
                    await _logService.LogErrorAsync($"Klasör bulunamadı: {folderId}", new KeyNotFoundException("Klasör bulunamadı"), userId);
                    throw new KeyNotFoundException("Klasör bulunamadı");
                }

                if (newParentFolderId != null)
                {
                    var newParentFolder = await _context.Folders
                        .FirstOrDefaultAsync(f => f.Id == newParentFolderId && f.UserId == userId);

                    if (newParentFolder == null)
                    {
                        await _logService.LogErrorAsync($"Hedef klasör bulunamadı: {newParentFolderId}", new KeyNotFoundException("Hedef klasör bulunamadı"), userId);
                        throw new KeyNotFoundException("Hedef klasör bulunamadı");
                    }

                    // Döngüsel referans kontrolü
                    if (await IsCircularReferenceAsync(folderId, newParentFolderId))
                    {
                        await _logService.LogErrorAsync("Döngüsel klasör referansı tespit edildi", new InvalidOperationException("Döngüsel klasör referansı"), userId);
                        throw new InvalidOperationException("Döngüsel klasör referansı tespit edildi");
                    }
                }

                folder.ParentFolderId = newParentFolderId;
                folder.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Önbelleği temizle
                _cacheService.Remove($"user_folders_{userId}");
                _cacheService.Remove($"user_folders_{userId}_{folder.ParentFolderId}");
                if (newParentFolderId != null)
                {
                    _cacheService.Remove($"user_folders_{userId}_{newParentFolderId}");
                }

                await _logService.LogInfoAsync($"Klasör başarıyla taşındı. ID: {folderId}", userId);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Klasör taşıma hatası: {ex.Message}", ex, userId);
                throw;
            }
        }

        private async Task<bool> IsCircularReferenceAsync(string folderId, string newParentFolderId)
        {
            var currentFolderId = newParentFolderId;
            while (currentFolderId != null)
            {
                if (currentFolderId == folderId)
                    return true;

                var parentFolder = await _context.Folders
                    .FirstOrDefaultAsync(f => f.Id == currentFolderId);

                if (parentFolder == null)
                    break;

                currentFolderId = parentFolder.ParentFolderId;
            }

            return false;
        }
    }
} 