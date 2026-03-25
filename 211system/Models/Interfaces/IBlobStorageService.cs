namespace _211system.Models.Interfaces
{
    public interface IBlobStorageService
    {
        Task<string> UploadAsync(IFormFile file, string containerName);
        Task<bool> DeleteAsync(string fileUrl, string containerName);
    }
}
