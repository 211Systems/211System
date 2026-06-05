using _211system.Data;
using _211system.Models.Interfaces;
using CPR112.Models;
using Microsoft.EntityFrameworkCore;

namespace _211system.Models.Services
{
    public class AttachmentService : IAttachmentService
    {
        public const int MaxAttachmentsPerIncident = 10;
        public const long MaxFileSizeBytes = 5 * 1024 * 1024;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf", ".docx", ".webp" };

        private readonly _211DbContext _context;
        private readonly IBlobStorageService _blobStorage;

        public AttachmentService(_211DbContext context, IBlobStorageService blobStorage)
        {
            _context = context;
            _blobStorage = blobStorage;
        }

        public async Task<Attachment> UploadFileAsync(IFormFile file, Guid incidentId)
        {
            ValidateFile(file);

            var count = await CountByIncidentAsync(incidentId);
            if (count >= MaxAttachmentsPerIncident)
                throw new ArgumentException($"Maksymalnie {MaxAttachmentsPerIncident} załączników na zgłoszenie.");

            var blobUrl = await _blobStorage.UploadAsync(file, "incidents");

            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                PathToFile = blobUrl,
                FileName = file.FileName,
                ContentType = file.ContentType ?? "application/octet-stream",
                FileSizeBytes = file.Length,
                UploadedAt = DateTime.UtcNow,
                IncidentId = incidentId
            };

            await _context.Attachments.AddAsync(attachment);
            await _context.SaveChangesAsync();

            return attachment;
        }

        public async Task<IReadOnlyList<AttachmentDto>> GetByIncidentAsync(Guid incidentId)
        {
            return await _context.Attachments
                .Where(a => a.IncidentId == incidentId)
                .OrderBy(a => a.UploadedAt)
                .Select(a => new AttachmentDto
                {
                    Id = a.Id,
                    Url = a.PathToFile,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    FileSizeBytes = a.FileSizeBytes,
                    UploadedAt = a.UploadedAt
                })
                .ToListAsync();
        }

        public async Task<int> CountByIncidentAsync(Guid incidentId)
        {
            return await _context.Attachments.CountAsync(a => a.IncidentId == incidentId);
        }

        public async Task<bool> DeleteAsync(Guid attachmentId)
        {
            var att = await _context.Attachments.FindAsync(attachmentId);
            if (att == null) return false;

            try { await _blobStorage.DeleteAsync(att.PathToFile, "incidents"); } catch { }

            _context.Attachments.Remove(att);
            await _context.SaveChangesAsync();
            return true;
        }

        private static void ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Plik jest pusty.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new ArgumentException("Niedozwolony typ pliku. Dozwolone: JPG, PNG, PDF, DOCX, WEBP.");

            if (file.Length > MaxFileSizeBytes)
                throw new ArgumentException("Plik przekracza limit 5 MB.");
        }
    }
}
