using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ShareVault.API.Data;
using ShareVault.API.Models;
using Microsoft.AspNetCore.Http;
using ShareVault.API.Interfaces;

namespace ShareVault.API.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly ILogService _logService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenService(IConfiguration configuration, AppDbContext context, ILogService logService, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _context = context;
            _logService = logService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> GenerateTokenAsync(User user)
        {
            var userRoles = await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["Jwt:ExpireDays"]));

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateJwtToken(User user)
        {
            return GenerateTokenAsync(user).GetAwaiter().GetResult();
        }

        public RefreshToken GenerateRefreshToken(User user)
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid().ToString(),
                Token = Convert.ToBase64String(randomNumber),
                UserId = user.Id,
                User = user,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                IpAddress = GetIpAddress(),
                UserAgent = GetUserAgent()
            };

            _context.RefreshTokens.Add(refreshToken);
            _context.SaveChanges();

            return refreshToken;
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow);
            if (refreshToken == null)
                return null;

            var currentIp = GetIpAddress();
            var currentUserAgent = GetUserAgent();
            if ((refreshToken.IpAddress != null && refreshToken.IpAddress != currentIp) ||
                (refreshToken.UserAgent != null && refreshToken.UserAgent != currentUserAgent))
            {
                refreshToken.IsRevoked = true;
                refreshToken.RevokedReason = "Şüpheli refresh token kullanımı: IP veya UserAgent değişti.";
                await _context.SaveChangesAsync();
                await _logService.LogSecurityAsync(
                    $"Şüpheli refresh token kullanımı tespit edildi. Token: {token}, Eski IP: {refreshToken.IpAddress}, Yeni IP: {currentIp}, Eski UA: {refreshToken.UserAgent}, Yeni UA: {currentUserAgent}",
                    refreshToken.UserId
                );
                return null;
            }
            return refreshToken;
        }

        public async Task RevokeRefreshTokenAsync(string token, string reason = "Token revoked")
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                refreshToken.RevokedReason = reason;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RevokeAllUserRefreshTokensAsync(string userId, string reason = "User logged out")
        {
            var userTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in userTokens)
            {
                token.IsRevoked = true;
                token.RevokedReason = reason;
            }

            await _context.SaveChangesAsync();
        }

        private string? GetIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        }

        private string? GetUserAgent()
        {
            return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
        }
    }
}