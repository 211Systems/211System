using CPR112.Models;

namespace _211system.Models.Interfaces
{
    public class AttachmentDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public interface IAttachmentService
    {
        Task<Attachment> UploadFileAsync(IFormFile file, Guid incidentId);
        Task<IReadOnlyList<AttachmentDto>> GetByIncidentAsync(Guid incidentId);
        Task<int> CountByIncidentAsync(Guid incidentId);
        Task<bool> DeleteAsync(Guid attachmentId);
    }
}
