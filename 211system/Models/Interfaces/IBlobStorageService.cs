namespace _211system.Models.Interfaces
{
    public interface IBlobStorageService
    {
        Task<string> UploadAsync(IFormFile file, string containerName);
        Task<bool> DeleteAsync(string fileUrl, string containerName);
        string GetSecureFileUrl(string fileUrl, string containerName, int expireMinutes = 15);
    }
}
