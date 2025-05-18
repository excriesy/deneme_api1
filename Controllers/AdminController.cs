using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareVault.API.Data;
using ShareVault.API.Models;
using ShareVault.API.Services;
using System.Security.Claims;
using ShareVault.API.DTOs;
using ShareVault.API.Interfaces;

namespace ShareVault.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IUserService _userService;
        private readonly ILogService _logService;

        public AdminController(AppDbContext context, IUserService userService, ILogService logService)
        {
            _context = context;
            _userService = userService;
            _logService = logService;
        }

        /// <summary>
        /// Tüm dosya paylaşım geçmişini listeler.
        /// </summary>
        [HttpGet("share-history")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetShareHistory()
        {
            try
            {
                var shareHistory = await _context.SharedFiles
                    .Include(sf => sf.File)
                    .Include(sf => sf.SharedByUser)
                    .Include(sf => sf.SharedWithUser)
                    .OrderByDescending(sf => sf.SharedAt)
                    .Select(sf => new
                    {
                        FileId = sf.FileId,
                        FileName = sf.File.Name,
                        SharedBy = new
                        {
                            Id = sf.SharedByUser.Id,
                            Username = sf.SharedByUser.Username,
                            Email = sf.SharedByUser.Email
                        },
                        SharedWith = new
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
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("Paylaşım geçmişi alınırken hata oluştu", ex);
                return StatusCode(500, "Paylaşım geçmişi alınırken bir hata oluştu");
            }
        }

        /// <summary>
        /// Sistem istatistiklerini getirir.
        /// </summary>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
                var totalFiles = await _context.Files.CountAsync();
                var totalShares = await _context.SharedFiles.CountAsync();
                var activeShares = await _context.SharedFiles.CountAsync(s => s.IsActive);

                var fileSizeStats = await _context.Files
                    .GroupBy(f => f.Size / (1024 * 1024)) // MB cinsinden
                    .Select(g => new
                    {
                        SizeRange = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(s => s.SizeRange)
                    .ToListAsync();

                var recentActivity = await _context.SharedFiles
                    .OrderByDescending(sf => sf.SharedAt)
                    .Take(10)
                    .Select(sf => new
                    {
                        FileName = sf.File.Name,
                        SharedBy = sf.SharedByUser.Username,
                        SharedWith = sf.SharedWithUser.Username,
                        SharedAt = sf.SharedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Users = new
                    {
                        Total = totalUsers,
                        Active = activeUsers,
                        Inactive = totalUsers - activeUsers
                    },
                    Files = new
                    {
                        Total = totalFiles,
                        SizeDistribution = fileSizeStats
                    },
                    Shares = new
                    {
                        Total = totalShares,
                        Active = activeShares,
                        Inactive = totalShares - activeShares
                    },
                    RecentActivity = recentActivity
                });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("İstatistikler alınırken hata oluştu", ex);
                return StatusCode(500, "İstatistikler alınırken bir hata oluştu");
            }
        }

        /// <summary>
        /// Belirli bir tarih aralığındaki paylaşım geçmişini getirir.
        /// </summary>
        [HttpGet("share-history/date-range")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetShareHistoryByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var shareHistory = await _context.SharedFiles
                    .Include(sf => sf.File)
                    .Include(sf => sf.SharedByUser)
                    .Include(sf => sf.SharedWithUser)
                    .Where(sf => sf.SharedAt >= startDate && sf.SharedAt <= endDate)
                    .OrderByDescending(sf => sf.SharedAt)
                    .Select(sf => new
                    {
                        FileId = sf.FileId,
                        FileName = sf.File.Name,
                        SharedBy = new
                        {
                            Id = sf.SharedByUser.Id,
                            Username = sf.SharedByUser.Username,
                            Email = sf.SharedByUser.Email
                        },
                        SharedWith = new
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
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalShares = shareHistory.Count,
                    ActiveShares = shareHistory.Count(s => s.IsActive),
                    InactiveShares = shareHistory.Count(s => !s.IsActive),
                    ShareHistory = shareHistory
                });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("Tarih aralığı paylaşım geçmişi alınırken hata oluştu", ex);
                return StatusCode(500, "Paylaşım geçmişi alınırken bir hata oluştu");
            }
        }
    }
} 