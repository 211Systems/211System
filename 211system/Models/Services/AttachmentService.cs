using _211system.Data;
using _211system.Models.Interfaces;
using CPR112.Models;

namespace _211system.Models.Services
{
    public class AttachmentService : IAttachmentService
    {
        private readonly _211DbContext _context;
        private readonly string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "attachments");

        public AttachmentService(_211DbContext context)
        {
            _context = context;
        }

        public Task<Attachment> UploadFileAsync(IFormFile file, Guid incidentId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".docx" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException("Unsupported file type.");

            if(file.Length> 5 * 1024 * 1024) 
                throw new ArgumentException("File size exceeds the limit.");

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(path, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyToAsync(stream);
            }
            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                PathToFile = $"/attachments/{fileName}",
                IncidentId = incidentId
            };


            _context.Attachments.AddAsync(attachment);
            _context.SaveChangesAsync();

            return Task.FromResult(attachment);
        }
    }
}
