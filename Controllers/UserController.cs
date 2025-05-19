using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareVault.API.Data;
using ShareVault.API.Models;
using ShareVault.API.Services;
using ShareVault.API.Interfaces;
using System.Security.Claims;

namespace ShareVault.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogService _logService;

        public UserController(IUserService userService, ILogService logService)
        {
            _userService = userService;
            _logService = logService;
        }

        [HttpGet("by-email/{email}")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            try
            {
                var user = await _userService.GetByEmailAsync(email);
                return Ok(new { id = user.Id, email = user.Email, username = user.Username });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync($"Error getting user by email: {email}", ex, null);
                return NotFound(new { message = "Kullanıcı bulunamadı" });
            }
        }
    }
} 