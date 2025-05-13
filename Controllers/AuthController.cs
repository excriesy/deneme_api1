using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareVault.API.Data;
using ShareVault.API.DTOs;
using ShareVault.API.Models;
using ShareVault.API.Services;
using System.Security.Cryptography;
using System.Text;

namespace ShareVault.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;

        public AuthController(AppDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest("Bu e-posta zaten kayıtlı.");

            // Şifre hash'leme
            using var sha256 = SHA256.Create();
            var passwordBytes = Encoding.UTF8.GetBytes(request.Password);
            var hashedPasswordBytes = sha256.ComputeHash(passwordBytes);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = hashedPasswordBytes // Doğrudan byte[] olarak atanıyor
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Burada rol işlemlerini veritabanınıza göre ayrıca yapmanız gerekir

            var token = _tokenService.CreateToken(user);
            var response = new AuthResponse
            {
                Token = token,
                Username = user.Username
            };

            return Ok(response);
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Kullanıcı adı veya e-posta ile kullanıcıyı bul
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == request.Username ||
                    u.Email == request.Username);

            if (user == null)
                return Unauthorized("Kullanıcı bulunamadı.");

            // Şifre doğrulama
            using var sha256 = SHA256.Create();
            var computedHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(request.Password));

            // byte[] dizisinin karşılaştırılması
            if (!ByteArraysEqual(computedHash, user.PasswordHash))
                return Unauthorized("Şifre hatalı.");

            var token = _tokenService.CreateToken(user);
            var response = new AuthResponse
            {
                Token = token,
                Username = user.Username
            };

            return Ok(response);
        }

        // byte[] dizilerinin karşılaştırılması için yardımcı metod
        private bool ByteArraysEqual(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length)
                return false;

            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i])
                    return false;
            }

            return true;
        }
    }
}
