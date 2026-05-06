using System;
using System.Threading.Tasks;
using _211system.DTOs;
using _211system.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _211system.Models.Interfaces;

namespace _211system.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var token = await _authService.LoginAsync(dto);
                return Ok(new { Token = token });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                await _authService.ChangePasswordAsync(dto);
                return Ok(new { Message = "Hasło zostało pomyślnie zmienione." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }


        [HttpGet("status/{email}")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Komendant, Naczelnik")]
        public async Task<IActionResult> GetLockStatus(string email)
        {
            var isLocked = await _authService.IsAccountLockedAsync(email);
            return Ok(isLocked);
        }

        [HttpPost("lock/{email}")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Komendant, Naczelnik")]
        public async Task<IActionResult> LockAccount(string email)
        {
            await _authService.LockAccountAsync(email);
            return Ok(new { message = "Konto zostało pomyślnie zablokowane." });
        }

        [HttpPost("unlock/{email}")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Komendant, Naczelnik")]
        public async Task<IActionResult> UnlockAccount(string email)
        {
            try
            {
                var newPassword = await _authService.UnlockAccountAsync(email);
                return Ok(new { newPassword });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}