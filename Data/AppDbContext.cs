using Microsoft.EntityFrameworkCore;
using ShareVault.API.Models;

namespace ShareVault.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<FileEntity> Files => Set<FileEntity>();
        public DbSet<SharedFile> SharedFiles => Set<SharedFile>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });
                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SharedFile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.File)
                    .WithMany()
                    .HasForeignKey(e => e.FileId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.SharedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.SharedByUserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.SharedWithUser)
                    .WithMany()
                    .HasForeignKey(e => e.SharedWithUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FileEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Size).IsRequired();
                entity.Property(e => e.UploadDate).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
            });
        }
    }
}
