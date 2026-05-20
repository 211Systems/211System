using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using _211system.Models;
using _211system.DTOs;
using _211system.Models.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace _211system.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        public async Task<(string AccountId, string TemporaryPassword)> CreateTemporaryAccountAsync(string email, string role)
        {
            var user = new ApplicationUser { UserName = email, Email = email, LockoutEnabled = true };

            int secureRandomNum = RandomNumberGenerator.GetInt32(1000, 10000);
            var tempPassword = $"Temp{secureRandomNum}";

            var result = await _userManager.CreateAsync(user, tempPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Błąd tworzenia konta: {errors}");
            }

            var roleExists = await _roleManager.RoleExistsAsync(role);
            if (roleExists) await _userManager.AddToRoleAsync(user, role);
            else throw new Exception($"Rola '{role}' nie istnieje w bazie danych! Sprawdź Seeder.");

            return (user.Id, tempPassword);
        }

        public async Task<string> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) throw new UnauthorizedAccessException("Nieprawidłowy email lub hasło.");

            if (await _userManager.IsLockedOutAsync(user))
                throw new UnauthorizedAccessException("Konto jest ZABLOKOWANE. Skontaktuj się z przełożonym.");

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                await _userManager.AccessFailedAsync(user);

                if (user.AccessFailedCount >= 3)
                {
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                    throw new UnauthorizedAccessException("Konto zostało ZABLOKOWANE z powodu 3 nieudanych prób logowania.");
                }

                throw new UnauthorizedAccessException($"Nieprawidłowy email lub hasło. Pozostało prób: {3 - user.AccessFailedCount}");
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            return GenerateJwtToken(user, roles);
        }

        public async Task<string> RefreshTokenAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) throw new Exception("Nie znaleziono użytkownika.");

            if (await _userManager.IsLockedOutAsync(user))
                throw new UnauthorizedAccessException("Konto zablokowane.");

            var roles = await _userManager.GetRolesAsync(user);
            return GenerateJwtToken(user, roles);
        }

        public string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("AvatarUrl", user.AvatarUrl ?? "")
            };

            foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task ChangePasswordAsync(ChangePasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) throw new Exception("Nie znaleziono użytkownika.");

            var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Błąd zmiany hasła: {errors}");
            }
        }

        public async Task<bool> IsAccountLockedAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;
            return await _userManager.IsLockedOutAsync(user);
        }

        public async Task LockAccountAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }
        }

        public async Task<string> UnlockAccountAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) throw new Exception("Nie znaleziono użytkownika.");

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            int secureRandomNum = RandomNumberGenerator.GetInt32(1000, 10000);
            var newTempPassword = $"Temp{secureRandomNum}";

            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, newTempPassword);

            if (!result.Succeeded) throw new Exception("Błąd podczas generowania nowego hasła.");

            return newTempPassword;
        }
    }
}