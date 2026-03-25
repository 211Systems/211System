using System.Security.Claims;
using _211system.Data;
using _211system.Models.Interfaces;
using _211system.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace _211system.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IBlobStorageService _blobStorageService;
        private readonly _211DbContext _context;

        public ProfileController(IBlobStorageService blobStorageService, _211DbContext context)
        {
            _blobStorageService = blobStorageService;
            _context = context;
        }

        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Nie wybrano pliku." });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { message = "Dozwolone są tylko pliki graficzne (jpg, png, gif)." });

            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized(new { message = "Brak autoryzacji." });

                string avatarUrl = await _blobStorageService.UploadAsync(file, "avatars");

                var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
                if (user != null)
                    
                {
                    if (!string.IsNullOrEmpty(user.AvatarUrl))
                    {
                        await _blobStorageService.DeleteAsync(user.AvatarUrl, "avatars");
                    }

                    user.AvatarUrl = avatarUrl;
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "Avatar został pomyślnie zaktualizowany.",
                    avatarUrl = avatarUrl
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Wystąpił błąd podczas wgrywania pliku.", details = ex.Message });
            }
        }
    }
}