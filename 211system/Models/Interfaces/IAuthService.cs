using _211system.DTOs;

namespace _211system.Models.Interfaces;

public interface IAuthService
{
Task<(string AccountId, string TemporaryPassword)> CreateTemporaryAccountAsync(string email, string role);
Task<string> LoginAsync(LoginDto dto);
Task ChangePasswordAsync(ChangePasswordDto dto);
Task<bool> IsAccountLockedAsync(string email);
Task LockAccountAsync(string email);
Task<string> UnlockAccountAsync(string email);
}