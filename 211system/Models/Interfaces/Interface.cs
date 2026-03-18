using _211system.DTOs;

public interface IAuthService
{
    Task<(string AccountId, string TemporaryPassword)> CreateTemporaryAccountAsync(string email, string role);
    Task<string> LoginAsync(LoginDto dto);
    Task ChangePasswordAsync(ChangePasswordDto dto);
}