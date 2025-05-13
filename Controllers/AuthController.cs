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

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto request)
        {
            if (await _context.Users.AnyAsync(x => x.Email == request.Email))
                return BadRequest("Email zaten kayıtlı.");

            using var hmac = new HMACSHA512();

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(request.Password)),
                PasswordSalt = hmac.Key,
                Role = "Editor"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _tokenService.CreateToken(user);

            return Ok(new
            {
                token,
                user.Email,
                user.FullName
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
            if (user == null) return Unauthorized("Kullanıcı bulunamadı.");

            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(request.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i])
                    return Unauthorized("Şifre hatalı.");
            }

            var token = _tokenService.CreateToken(user);

            return Ok(new
            {
                token,
                user.Email,
                user.FullName
            });
        }
    }
}
