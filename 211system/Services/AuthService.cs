using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using _211system.DTOs;
using _211system.Models.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace _211system.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        public async Task<(string AccountId, string TemporaryPassword)> CreateTemporaryAccountAsync(string email, string role)
        {
            var user = new IdentityUser { UserName = email, Email = email };

            int secureRandomNum = RandomNumberGenerator.GetInt32(1000, 10000);
            var tempPassword = $"Temp{secureRandomNum}";

            var result = await _userManager.CreateAsync(user, tempPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Błąd tworzenia konta: {errors}");
            }

            var roleExists = await _roleManager.RoleExistsAsync(role);
            if (roleExists)
            {
                await _userManager.AddToRoleAsync(user, role);
            }
            else
            {
                throw new Exception($"Rola '{role}' nie istnieje w bazie danych! Sprawdź Seeder.");
            }

            return (user.Id, tempPassword);
        }

        public async Task<string> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                throw new UnauthorizedAccessException("Nieprawidłowy email lub hasło.");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task ChangePasswordAsync(ChangePasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                throw new Exception("Nie znaleziono użytkownika.");
            }

            var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Błąd zmiany hasła: {errors}");
            }
        }
    }
}