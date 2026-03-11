using CPR112.Models;

namespace _211system.Models.Interfaces
{
    public interface IAttachmentService
    {
        Task<Attachment> UploadFileAsync(IFormFile file, Guid incidentId);
    }
}
