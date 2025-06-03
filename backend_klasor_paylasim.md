# ShareVault API - Backend Klasör Paylaşım Sistemi

# ShareVault API - Backend Klasör Paylaşım Sistemi

## 1. FolderShare Model

**Dosya:** `Models/FolderShare.cs` (Yeni dosya oluştur)

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareVault.API.Models
{
    public class FolderShare
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FolderId { get; set; }
        
        [ForeignKey("FolderId")]
        public virtual Folder Folder { get; set; }

        [Required]
        public int SharedWithUserId { get; set; }
        
        [ForeignKey("SharedWithUserId")]
        public virtual User SharedWithUser { get; set; }

        [Required]
        public int SharedByUserId { get; set; }
        
        [ForeignKey("SharedByUserId")]
        public virtual User SharedByUser { get; set; }

        [Required]
        [StringLength(20)]
        public string Permission { get; set; } // "Read", "Write", "Owner"

        [Required]
        public DateTime SharedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
```

## 2. FolderController Updates

**Dosya:** `Controllers/FolderController.cs` (Mevcut dosyaya ekle)

```csharp
// Mevcut using'lere ekle:
using Microsoft.EntityFrameworkCore;
using System.Linq;

// FolderController class'ına bu metodları ekle:

// POST /api/folder/{id}/share - Klasör paylaş
[HttpPost("{id}/share")]
[Authorize]
public async Task<IActionResult> ShareFolder(int id, [FromBody] ShareFolderDto shareDto)
{
    var userId = int.Parse(User.FindFirst("UserId").Value);
    
    // Klasörün sahibi mi kontrol et
    var folder = await _context.Folders
        .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
    
    if (folder == null)
    {
        return NotFound(new { message = "Folder not found or you don't have permission" });
    }

    // Kullanıcı var mı kontrol et
    var targetUser = await _context.Users
        .FirstOrDefaultAsync(u => u.Email == shareDto.UserEmail);
    
    if (targetUser == null)
    {
        return BadRequest(new { message = "User not found" });
    }

    // Kendine paylaşım kontrolü
    if (targetUser.Id == userId)
    {
        return BadRequest(new { message = "You cannot share with yourself" });
    }

    // Zaten paylaşılmış mı kontrol et
    var existingShare = await _context.FolderShares
        .FirstOrDefaultAsync(fs => fs.FolderId == id && 
                                   fs.SharedWithUserId == targetUser.Id && 
                                   fs.IsActive);
    
    if (existingShare != null)
    {
        return BadRequest(new { message = "Folder already shared with this user" });
    }

    var folderShare = new FolderShare
    {
        FolderId = id,
        SharedWithUserId = targetUser.Id,
        SharedByUserId = userId,
        Permission = shareDto.Permission ?? "Read",
        SharedAt = DateTime.UtcNow,
        ExpiresAt = shareDto.ExpiresAt,
        IsActive = true
    };

    _context.FolderShares.Add(folderShare);
    await _context.SaveChangesAsync();

    return Ok(new 
    { 
        message = "Folder shared successfully",
        shareId = folderShare.Id,
        sharedWith = targetUser.Email,
        permission = folderShare.Permission
    });
}

// GET /api/folder/shared - Paylaşılan klasörlerim
[HttpGet("shared")]
[Authorize]
public async Task<IActionResult> GetSharedFolders()
{
    var userId = int.Parse(User.FindFirst("UserId").Value);
    
    var sharedFolders = await _context.FolderShares
        .Include(fs => fs.Folder)
        .Include(fs => fs.SharedByUser)
        .Where(fs => fs.SharedWithUserId == userId && 
                     fs.IsActive && 
                     (fs.ExpiresAt == null || fs.ExpiresAt > DateTime.UtcNow))
        .Select(fs => new
        {
            shareId = fs.Id,
            folderId = fs.Folder.Id,
            folderName = fs.Folder.Name,
            sharedBy = fs.SharedByUser.Username,
            sharedByEmail = fs.SharedByUser.Email,
            permission = fs.Permission,
            sharedAt = fs.SharedAt,
            expiresAt = fs.ExpiresAt
        })
        .ToListAsync();

    return Ok(sharedFolders);
}

// DELETE /api/folder/{id}/unshare/{userId} - Paylaşımı kaldır
[HttpDelete("{id}/unshare/{targetUserId}")]
[Authorize]
public async Task<IActionResult> UnshareFolder(int id, int targetUserId)
{
    var userId = int.Parse(User.FindFirst("UserId").Value);
    
    // Klasörün sahibi mi kontrol et
    var folder = await _context.Folders
        .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
    
    if (folder == null)
    {
        return NotFound(new { message = "Folder not found or you don't have permission" });
    }

    var folderShare = await _context.FolderShares
        .FirstOrDefaultAsync(fs => fs.FolderId == id && 
                                   fs.SharedWithUserId == targetUserId && 
                                   fs.IsActive);
    
    if (folderShare == null)
    {
        return NotFound(new { message = "Share not found" });
    }

    folderShare.IsActive = false;
    await _context.SaveChangesAsync();

    return Ok(new { message = "Folder unshared successfully" });
}

// DTO class - FolderController.cs dosyasının sonuna ekle (namespace içinde)
public class ShareFolderDto
{
    [Required]
    [EmailAddress]
    public string UserEmail { get; set; }
    
    [RegularExpression("^(Read|Write|Owner)$", ErrorMessage = "Permission must be Read, Write, or Owner")]
    public string Permission { get; set; } = "Read";
    
    public DateTime? ExpiresAt { get; set; }
}
```

## 3. Migration Script

**Manuel olarak çalıştırılacak SQL:**

```sql
-- FolderShares tablosu oluştur
CREATE TABLE FolderShares (
    Id INT PRIMARY KEY IDENTITY(1,1),
    FolderId INT NOT NULL,
    SharedWithUserId INT NOT NULL,
    SharedByUserId INT NOT NULL,
    Permission NVARCHAR(20) NOT NULL,
    SharedAt DATETIME2 NOT NULL,
    ExpiresAt DATETIME2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    CONSTRAINT FK_FolderShares_Folders FOREIGN KEY (FolderId) 
        REFERENCES Folders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_FolderShares_SharedWithUser FOREIGN KEY (SharedWithUserId) 
        REFERENCES Users(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_FolderShares_SharedByUser FOREIGN KEY (SharedByUserId) 
        REFERENCES Users(Id) ON DELETE NO ACTION
);

-- Index'ler için
CREATE INDEX IX_FolderShares_FolderId ON FolderShares(FolderId);
CREATE INDEX IX_FolderShares_SharedWithUserId ON FolderShares(SharedWithUserId);
CREATE INDEX IX_FolderShares_SharedByUserId ON FolderShares(SharedByUserId);
CREATE INDEX IX_FolderShares_IsActive ON FolderShares(IsActive);

-- Unique constraint - aynı klasör aynı kullanıcıya birden fazla aktif paylaşım olmasın
CREATE UNIQUE INDEX IX_FolderShares_Unique_Active 
ON FolderShares(FolderId, SharedWithUserId) 
WHERE IsActive = 1;
```

## 4. ApplicationDbContext Update

**Dosya:** `Data/ApplicationDbContext.cs` (Mevcut dosyaya ekle)

```csharp
// DbSet ekle (diğer DbSet'lerin yanına)
public DbSet<FolderShare> FolderShares { get; set; }

// OnModelCreating metoduna ekle
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Mevcut kodlarınız...

    // FolderShare configurations
    modelBuilder.Entity<FolderShare>(entity =>
    {
        entity.HasKey(e => e.Id);
        
        entity.Property(e => e.Permission)
            .IsRequired()
            .HasMaxLength(20);
        
        entity.Property(e => e.SharedAt)
            .IsRequired();
        
        entity.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Foreign key relationships
        entity.HasOne(e => e.Folder)
            .WithMany()
            .HasForeignKey(e => e.FolderId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.SharedWithUser)
            .WithMany()
            .HasForeignKey(e => e.SharedWithUserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(e => e.SharedByUser)
            .WithMany()
            .HasForeignKey(e => e.SharedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes
        entity.HasIndex(e => e.FolderId);
        entity.HasIndex(e => e.SharedWithUserId);
        entity.HasIndex(e => e.SharedByUserId);
        entity.HasIndex(e => e.IsActive);
        
        // Unique constraint
        entity.HasIndex(e => new { e.FolderId, e.SharedWithUserId })
            .HasFilter("IsActive = 1")
            .IsUnique();
    });
}
```

## 5. Postman Collection

```json
{
    "info": {
        "name": "ShareVault Folder Sharing",
        "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
    },
    "item": [
        {
            "name": "Share Folder",
            "request": {
                "method": "POST",
                "header": [
                    {
                        "key": "Authorization",
                        "value": "Bearer {{token}}",
                        "type": "text"
                    },
                    {
                        "key": "Content-Type",
                        "value": "application/json",
                        "type": "text"
                    }
                ],
                "body": {
                    "mode": "raw",
                    "raw": "{\n    \"userEmail\": \"test@example.com\",\n    \"permission\": \"Read\",\n    \"expiresAt\": \"2024-12-31T23:59:59Z\"\n}"
                },
                "url": {
                    "raw": "{{baseUrl}}/api/folder/1/share",
                    "host": ["{{baseUrl}}"],
                    "path": ["api", "folder", "1", "share"]
                }
            }
        },
        {
            "name": "Get Shared Folders",
            "request": {
                "method": "GET",
                "header": [
                    {
                        "key": "Authorization",
                        "value": "Bearer {{token}}",
                        "type": "text"
                    }
                ],
                "url": {
                    "raw": "{{baseUrl}}/api/folder/shared",
                    "host": ["{{baseUrl}}"],
                    "path": ["api", "folder", "shared"]
                }
            }
        },
        {
            "name": "Unshare Folder",
            "request": {
                "method": "DELETE",
                "header": [
                    {
                        "key": "Authorization",
                        "value": "Bearer {{token}}",
                        "type": "text"
                    }
                ],
                "url": {
                    "raw": "{{baseUrl}}/api/folder/1/unshare/2",
                    "host": ["{{baseUrl}}"],
                    "path": ["api", "folder", "1", "unshare", "2"]
                }
            }
        }
    ],
    "variable": [
        {
            "key": "baseUrl",
            "value": "https://localhost:7000",
            "type": "string"
        },
        {
            "key": "token",
            "value": "YOUR_JWT_TOKEN_HERE",
            "type": "string"
        }
    ]
}
```

## Kurulum Adımları:

1. `Models/FolderShare.cs` dosyasını oluştur ve kodu yapıştır
2. `Controllers/FolderController.cs` dosyasına yeni metodları ekle
3. `Data/ApplicationDbContext.cs` dosyasını güncelle
4. SQL script'ini veritabanında çalıştır
5. Projeyi derle ve çalıştır

## Test Senaryosu:

1. Login ol ve JWT token al
2. Bir klasör oluştur
3. Bu klasörü başka bir kullanıcıyla paylaş (email ile)
4. Paylaşılan kullanıcı olarak login ol
5. `/api/folder/shared` endpoint'ini çağırarak paylaşılan klasörleri gör
6. İlk kullanıcı olarak paylaşımı kaldır