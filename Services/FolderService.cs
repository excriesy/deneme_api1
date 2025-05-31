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
        private readonly string SHARED_FOLDERS_CACHE_KEY = "shared_folders_";
        private readonly string FOLDER_SHARES_CACHE_KEY = "folder_shares_";

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

        public async Task<SharedFolderDto> ShareFolderAsync(string folderId, string sharedByUserId, string sharedWithUserId, PermissionType permission = PermissionType.Read, DateTime? expiresAt = null, string? shareNote = null)
        {
            try
            {
                await _logService.LogInfoAsync($"Klasör paylaşımı başlatıldı. Klasör: {folderId}, Paylaşan: {sharedByUserId}, Alıcı: {sharedWithUserId}", sharedByUserId);

                // Klasörün varlığını ve kullanıcının sahipliğini kontrol et
                var folder = await _context.Folders
                    .FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == sharedByUserId);

                if (folder == null)
                {
                    await _logService.LogErrorAsync($"Klasör bulunamadı veya kullanıcıya ait değil: {folderId}", 
                        new KeyNotFoundException("Klasör bulunamadı veya kullanıcıya ait değil"), sharedByUserId);
                    throw new KeyNotFoundException("Klasör bulunamadı veya kullanıcıya ait değil");
                }

                // Paylaşım alıcısını kontrol et
                var sharedWithUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == sharedWithUserId);

                if (sharedWithUser == null)
                {
                    await _logService.LogErrorAsync($"Paylaşım yapılacak kullanıcı bulunamadı: {sharedWithUserId}", 
                        new KeyNotFoundException("Paylaşım yapılacak kullanıcı bulunamadı"), sharedByUserId);
                    throw new KeyNotFoundException("Paylaşım yapılacak kullanıcı bulunamadı");
                }

                // Önceden paylaşım var mı kontrol et
                var existingShare = await _context.SharedFolders
                    .FirstOrDefaultAsync(sf => sf.FolderId == folderId && sf.SharedWithUserId == sharedWithUserId && sf.IsActive);

                if (existingShare != null)
                {
                    // Eğer aktif bir paylaşım varsa, izinleri güncelle
                    existingShare.Permission = permission;
                    existingShare.ExpiresAt = expiresAt;
                    existingShare.ShareNote = shareNote;
                    existingShare.SharedAt = DateTime.UtcNow;
                    existingShare.IsActive = true;

                    await _context.SaveChangesAsync();

                    await _logService.LogInfoAsync($"Mevcut klasör paylaşımı güncellendi. ID: {existingShare.Id}", sharedByUserId);

                    // Önbelleği temizle
                    _cacheService.Remove($"{SHARED_FOLDERS_CACHE_KEY}{sharedWithUserId}");
                    _cacheService.Remove($"{FOLDER_SHARES_CACHE_KEY}{folderId}");

                    return new SharedFolderDto
                    {
                        Id = existingShare.Id,
                        FolderId = existingShare.FolderId,
                        FolderName = folder.Name,
                        SharedByUserId = existingShare.SharedByUserId,
                        SharedByUserName = existingShare.SharedByUser.Username,
                        SharedWithUserId = existingShare.SharedWithUserId,
                        SharedWithUserName = existingShare.SharedWithUser.Username,
                        SharedAt = existingShare.SharedAt,
                        ExpiresAt = existingShare.ExpiresAt,
                        Permission = existingShare.Permission,
                        IsActive = existingShare.IsActive,
                        ShareNote = existingShare.ShareNote,
                        LastAccessedAt = existingShare.LastAccessedAt,
                        FileCount = folder.Files.Count,
                        SubFolderCount = folder.SubFolders.Count
                    };
                }

                // Yeni paylaşım oluştur
                var sharedFolder = new SharedFolder
                {
                    Id = Guid.NewGuid().ToString(),
                    FolderId = folderId,
                    SharedByUserId = sharedByUserId,
                    SharedWithUserId = sharedWithUserId,
                    SharedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    Permission = permission,
                    IsActive = true,
                    ShareNote = shareNote,
                    Folder = folder,
                    SharedByUser = await _context.Users.FirstAsync(u => u.Id == sharedByUserId),
                    SharedWithUser = sharedWithUser
                };

                _context.SharedFolders.Add(sharedFolder);
                await _context.SaveChangesAsync();

                await _logService.LogInfoAsync($"Klasör başarıyla paylaşıldı. ID: {sharedFolder.Id}", sharedByUserId);

                // Önbelleği temizle
                _cacheService.Remove($"{SHARED_FOLDERS_CACHE_KEY}{sharedWithUserId}");
                _cacheService.Remove($"{FOLDER_SHARES_CACHE_KEY}{folderId}");

                return new SharedFolderDto
                {
                    Id = sharedFolder.Id,
                    FolderId = sharedFolder.FolderId,
                    FolderName = folder.Name,
                    SharedByUserId = sharedFolder.SharedByUserId,
                    SharedByUserName = sharedFolder.SharedByUser.Username,
                    SharedWithUserId = sharedFolder.SharedWithUserId,
                    SharedWithUserName = sharedFolder.SharedWithUser.Username,
                    SharedAt = sharedFolder.SharedAt,
                    ExpiresAt = sharedFolder.ExpiresAt,
                    Permission = sharedFolder.Permission,
                    IsActive = sharedFolder.IsActive,
                    ShareNote = sharedFolder.ShareNote,
                    LastAccessedAt = sharedFolder.LastAccessedAt,
                    FileCount = folder.Files.Count,
                    SubFolderCount = folder.SubFolders.Count
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Klasör paylaşım hatası: {ex.Message}", ex, sharedByUserId);
                throw;
            }
        }

        public async Task<IEnumerable<SharedFolderDto>> GetSharedFoldersAsync(string userId)
        {
            try
            {
                await _logService.LogInfoAsync($"Paylaşılan klasörleri listeleme başlatıldı. Kullanıcı: {userId}", userId);

                // Önbellekten kontrol et
                var cacheKey = $"{SHARED_FOLDERS_CACHE_KEY}{userId}";
                var cachedFolders = _cacheService.Get<List<SharedFolderDto>>(cacheKey);
                if (cachedFolders != null)
                {
                    await _logService.LogInfoAsync($"Önbellekten paylaşılan klasör listesi alındı. Kullanıcı: {userId}", userId);
                    return cachedFolders;
                }

                // Kullanıcı ile paylaşılan aktif klasörleri getir
                var sharedFolders = await _context.SharedFolders
                    .Include(sf => sf.Folder)
                    .Include(sf => sf.SharedByUser)
                    .Include(sf => sf.SharedWithUser)
                    .Where(sf => sf.SharedWithUserId == userId && sf.IsActive)
                    .ToListAsync();

                var sharedFolderDtos = sharedFolders.Select(sf => new SharedFolderDto
                {
                    Id = sf.Id,
                    FolderId = sf.FolderId,
                    FolderName = sf.Folder.Name,
                    SharedByUserId = sf.SharedByUserId,
                    SharedByUserName = sf.SharedByUser.Username,
                    SharedWithUserId = sf.SharedWithUserId,
                    SharedWithUserName = sf.SharedWithUser.Username,
                    SharedAt = sf.SharedAt,
                    ExpiresAt = sf.ExpiresAt,
                    Permission = sf.Permission,
                    IsActive = sf.IsActive,
                    ShareNote = sf.ShareNote,
                    LastAccessedAt = sf.LastAccessedAt,
                    FileCount = sf.Folder.Files.Count,
                    SubFolderCount = sf.Folder.SubFolders.Count
                }).ToList();

                // Önbelleğe kaydet
                _cacheService.Set(cacheKey, sharedFolderDtos, TimeSpan.FromMinutes(5));

                await _logService.LogInfoAsync($"Paylaşılan klasör listesi başarıyla alındı. Sayı: {sharedFolderDtos.Count}", userId);

                return sharedFolderDtos;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Paylaşılan klasörleri listeleme hatası: {ex.Message}", ex, userId);
                throw;
            }
        }

        public async Task<IEnumerable<SharedFolderDto>> GetFolderSharesAsync(string folderId, string userId)
        {
            try
            {
                await _logService.LogInfoAsync($"Klasör paylaşımlarını listeleme başlatıldı. Klasör: {folderId}, Kullanıcı: {userId}", userId);

                // Önbellekten kontrol et
                var cacheKey = $"{FOLDER_SHARES_CACHE_KEY}{folderId}";
                var cachedShares = _cacheService.Get<List<SharedFolderDto>>(cacheKey);
                if (cachedShares != null)
                {
                    await _logService.LogInfoAsync($"Önbellekten klasör paylaşımları alındı. Klasör: {folderId}", userId);
                    return cachedShares;
                }

                // Önce klasörün varlığını ve kullanıcının sahipliğini kontrol et
                var folder = await _context.Folders
                    .FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == userId);

                if (folder == null)
                {
                    await _logService.LogErrorAsync($"Klasör bulunamadı veya kullanıcıya ait değil: {folderId}", 
                        new KeyNotFoundException("Klasör bulunamadı veya kullanıcıya ait değil"), userId);
                    throw new KeyNotFoundException("Klasör bulunamadı veya kullanıcıya ait değil");
                }

                // Klasörün tüm paylaşımlarını getir
                var folderShares = await _context.SharedFolders
                    .Include(sf => sf.SharedByUser)
                    .Include(sf => sf.SharedWithUser)
                    .Where(sf => sf.FolderId == folderId)
                    .ToListAsync();

                var folderShareDtos = folderShares.Select(sf => new SharedFolderDto
                {
                    Id = sf.Id,
                    FolderId = sf.FolderId,
                    FolderName = folder.Name,
                    SharedByUserId = sf.SharedByUserId,
                    SharedByUserName = sf.SharedByUser.Username,
                    SharedWithUserId = sf.SharedWithUserId,
                    SharedWithUserName = sf.SharedWithUser.Username,
                    SharedAt = sf.SharedAt,
                    ExpiresAt = sf.ExpiresAt,
                    Permission = sf.Permission,
                    IsActive = sf.IsActive,
                    ShareNote = sf.ShareNote,
                    LastAccessedAt = sf.LastAccessedAt,
                    FileCount = folder.Files.Count,
                    SubFolderCount = folder.SubFolders.Count
                }).ToList();

                // Önbelleğe kaydet
                _cacheService.Set(cacheKey, folderShareDtos, TimeSpan.FromMinutes(5));

                await _logService.LogInfoAsync($"Klasör paylaşımları başarıyla alındı. Sayı: {folderShareDtos.Count}", userId);

                return folderShareDtos;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Klasör paylaşımlarını listeleme hatası: {ex.Message}", ex, userId);
                throw;
            }
        }

        public async Task RevokeFolderAccessAsync(string folderId, string sharedWithUserId, string userId)
        {
            try
            {
                await _logService.LogInfoAsync($"Klasör erişimi iptal etme başlatıldı. Klasör: {folderId}, Kullanıcı: {userId}, Paylaşılan kullanıcı: {sharedWithUserId}", userId);

                // Önce klasörün varlığını ve kullanıcının sahipliğini kontrol et
                var folder = await _context.Folders
                    .FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == userId);

                if (folder == null)
                {
                    await _logService.LogErrorAsync($"Klasör bulunamadı veya kullanıcıya ait değil: {folderId}", 
                        new KeyNotFoundException("Klasör bulunamadı veya kullanıcıya ait değil"), userId);
                    throw new KeyNotFoundException("Klasör bulunamadı veya kullanıcıya ait değil");
                }

                // Paylaşımı bul
                var sharedFolder = await _context.SharedFolders
                    .FirstOrDefaultAsync(sf => sf.FolderId == folderId && sf.SharedWithUserId == sharedWithUserId && sf.IsActive);

                if (sharedFolder == null)
                {
                    await _logService.LogErrorAsync($"Aktif paylaşım bulunamadı. Klasör: {folderId}, Paylaşılan kullanıcı: {sharedWithUserId}", 
                        new KeyNotFoundException("Aktif paylaşım bulunamadı"), userId);
                    throw new KeyNotFoundException("Aktif paylaşım bulunamadı");
                }

                // Paylaşımı devre dışı bırak
                sharedFolder.IsActive = false;
                await _context.SaveChangesAsync();

                // Önbelleği temizle
                _cacheService.Remove($"{SHARED_FOLDERS_CACHE_KEY}{sharedWithUserId}");
                _cacheService.Remove($"{FOLDER_SHARES_CACHE_KEY}{folderId}");

                await _logService.LogInfoAsync($"Klasör erişimi başarıyla iptal edildi. Paylaşım ID: {sharedFolder.Id}", userId);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Klasör erişimi iptal etme hatası: {ex.Message}", ex, userId);
                throw;
            }
        }

        public async Task<bool> HasAccessToFolderAsync(string folderId, string userId, PermissionType requiredPermission = PermissionType.Read)
        {
            try
            {
                // Kullanıcı klasörün sahibi mi?
                var isOwner = await _context.Folders
                    .AnyAsync(f => f.Id == folderId && f.UserId == userId);

                if (isOwner)
                {
                    return true; // Klasörün sahibi tam erişime sahiptir
                }

                // Kullanıcı ile klasör paylaşılmış mı?
                var sharedFolder = await _context.SharedFolders
                    .FirstOrDefaultAsync(sf => sf.FolderId == folderId && sf.SharedWithUserId == userId && sf.IsActive);

                if (sharedFolder == null)
                {
                    return false; // Paylaşım yok
                }

                // Paylaşım süresi dolmuş mu?
                if (sharedFolder.ExpiresAt.HasValue && sharedFolder.ExpiresAt.Value < DateTime.UtcNow)
                {
                    return false; // Paylaşım süresi dolmuş
                }

                // Kullanıcının paylaşım izni yeterli mi?
                // PermissionType, yüksek değerler daha fazla izni temsil edecek şekilde düzenlenmiş olmalı
                // Örneğin: FullControl > Share > Delete > Write > Read
                if ((int)sharedFolder.Permission < (int)requiredPermission)
                {
                    return false; // Yetersiz izin
                }

                // Erişim sayısını ve son erişim zamanını güncelle
                sharedFolder.AccessCount++;
                sharedFolder.LastAccessedAt = DateTime.UtcNow;
                sharedFolder.LastAccessedBy = userId;
                await _context.SaveChangesAsync();

                return true; // Erişim onaylandı
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Klasör erişim kontrolü hatası: {ex.Message}", ex, userId);
                return false; // Hata durumunda güvenli tarafta kal ve erişimi reddet
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