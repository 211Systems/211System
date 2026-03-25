using _211system.Models.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace _211system.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("AzureBlobStorage");
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadAsync(IFormFile file, string containerName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Plik jest pusty lub uszkodzony.");

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            var accessType = containerName.ToLower() == "avatars"
                ? PublicAccessType.Blob
                : PublicAccessType.None;

            await containerClient.CreateIfNotExistsAsync(accessType);

            var fileExtension = Path.GetExtension(file.FileName);
            var blobName = $"{Guid.NewGuid()}{fileExtension}";

            var blobClient = containerClient.GetBlobClient(blobName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

            return blobClient.Uri.ToString();
        }

        public async Task<bool> DeleteAsync(string fileUrl, string containerName)
        {
            if (string.IsNullOrEmpty(fileUrl)) return false;

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            var uri = new Uri(fileUrl);
            var blobName = Path.GetFileName(uri.LocalPath);

            var blobClient = containerClient.GetBlobClient(blobName);

            return await blobClient.DeleteIfExistsAsync();
        }
    }
}
