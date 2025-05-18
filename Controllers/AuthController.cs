using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareVault.API.Data;
using ShareVault.API.DTOs;
using ShareVault.API.Models;
using ShareVault.API.Services;
using System.Security.Claims;
using ShareVault.API.Interfaces;

namespace ShareVault.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IUserService _userService;
        private readonly ILogService _logService;
        private readonly IBruteForceProtectionService _bruteForceService;

        public AuthController(AppDbContext context, ITokenService tokenService, IUserService userService, ILogService logService, IBruteForceProtectionService bruteForceService)
        {
            _context = context;
            _tokenService = tokenService;
            _userService = userService;
            _logService = logService;
            _bruteForceService = bruteForceService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            try
            {
                var user = await _userService.RegisterAsync(registerDto);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            try
            {
                var user = await _userService.LoginAsync(loginDto);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _userService.GetByIdAsync(userId);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(RefreshTokenDto request)
        {
            try
            {
                var refreshToken = await _tokenService.GetRefreshTokenAsync(request.RefreshToken);
                if (refreshToken == null)
                {
                    return BadRequest(new { Message = "Invalid refresh token" });
                }

                var userDto = await _userService.GetByIdAsync(refreshToken.UserId);
                if (userDto == null)
                {
                    return BadRequest(new { Message = "User not found" });
                }

                var user = await _context.Users.FindAsync(userDto.Id);
                if (user == null)
                {
                    throw new InvalidOperationException("Kullanıcı bulunamadı.");
                }

                // Eski refresh token'ı iptal et
                await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, "Token refreshed");

                // Yeni token'ları oluştur
                var newToken = await _tokenService.GenerateTokenAsync(user);
                var newRefreshToken = _tokenService.GenerateRefreshToken(user);

                await _logService.LogRequestAsync("POST", "/api/auth/refresh", 200, user.Id);

                return Ok(new
                {
                    Token = newToken,
                    RefreshToken = newRefreshToken.Token
                });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(ex.Message, ex, null);
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != null)
                {
                    await _tokenService.RevokeAllUserRefreshTokensAsync(userId);
                    await _logService.LogRequestAsync("POST", "/api/auth/logout", 200, userId);
                }

                return Ok(new { Message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _logService.LogErrorAsync(ex.Message, ex, userId);
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
