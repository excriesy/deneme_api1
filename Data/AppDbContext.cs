using Microsoft.EntityFrameworkCore;
using ShareVault.API.Models;

namespace ShareVault.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Temel modeller
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        
        // Dosya ve klasör modelleri
        public DbSet<FileModel> Files { get; set; }
        public DbSet<FileVersion> FileVersions { get; set; }
        public DbSet<Folder> Folders { get; set; }
        
        // Paylaşım modelleri
        public DbSet<SharedFile> SharedFiles { get; set; }
        public DbSet<SharedFolder> SharedFolders { get; set; }
        
        // Bildirim modelleri
        public DbSet<Notification> Notifications { get; set; }
        
        // Sistem modelleri
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<LogEntry> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Kullanıcı ve Rol Yapılandırmaları
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.Department).HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.LastLoginIP).HasMaxLength(50);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });
                entity.Property(e => e.AssignedBy).HasMaxLength(50);
                entity.HasOne(e => e.User)
                    .WithMany(e => e.UserRoles)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Role)
                    .WithMany(e => e.UserRoles)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Dosya ve Klasör Yapılandırmaları
            modelBuilder.Entity<FileModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Path).IsRequired();
                entity.Property(e => e.Tags).HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.DeletedBy).HasMaxLength(50);
                entity.Property(e => e.EncryptionMethod).HasMaxLength(50);
                entity.Property(e => e.MetadataText).HasMaxLength(2000);
                entity.HasOne(e => e.UploadedBy)
                    .WithMany(e => e.Files)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            modelBuilder.Entity<FileVersion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Path).IsRequired();
                entity.Property(e => e.VersionNumber).HasMaxLength(20).IsRequired();
                entity.Property(e => e.ChangeNotes).HasMaxLength(1000);
                entity.HasOne(e => e.File)
                    .WithMany(e => e.Versions)
                    .HasForeignKey(e => e.FileId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Folder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Tags).HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.DeletedBy).HasMaxLength(50);
                entity.Property(e => e.MetadataText).HasMaxLength(2000);
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.ParentFolder)
                    .WithMany(e => e.SubFolders)
                    .HasForeignKey(e => e.ParentFolderId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
            });

            // Paylaşım Yapılandırmaları
            modelBuilder.Entity<SharedFile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ShareNote).HasMaxLength(500);
                entity.Property(e => e.LastAccessedBy).HasMaxLength(50);
                entity.HasOne(e => e.File)
                    .WithMany(e => e.SharedFiles)
                    .HasForeignKey(e => e.FileId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.SharedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.SharedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.SharedWithUser)
                    .WithMany()
                    .HasForeignKey(e => e.SharedWithUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            
            modelBuilder.Entity<SharedFolder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ShareNote).HasMaxLength(500);
                entity.Property(e => e.LastAccessedBy).HasMaxLength(50);
                entity.HasOne(e => e.Folder)
                    .WithMany(e => e.SharedFolders)
                    .HasForeignKey(e => e.FolderId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.SharedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.SharedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.SharedWithUser)
                    .WithMany()
                    .HasForeignKey(e => e.SharedWithUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Bildirim Yapılandırması
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.ActionLink).HasMaxLength(500);
                entity.HasOne(e => e.User)
                    .WithMany(e => e.Notifications)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Sistem Yapılandırmaları
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired();
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LogEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Level).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Timestamp).IsRequired();
            });
        }
    }
}
