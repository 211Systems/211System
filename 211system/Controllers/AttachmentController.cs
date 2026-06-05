using _211system.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _211system.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AttachmentController : Controller
    {
        private readonly IAttachmentService _attachmentService;
        private readonly IBlobStorageService _blobStorage;

        public AttachmentController(IAttachmentService attachmentService, IBlobStorageService blobStorage)
        {
            _attachmentService = attachmentService;
            _blobStorage = blobStorage;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadAttachment(IFormFile file, [FromQuery] Guid incidentId)
        {
            try
            {
                var result = await _attachmentService.UploadFileAsync(file, incidentId);
                var secureUrl = _blobStorage.GetSecureFileUrl(result.PathToFile, "incidents");
                return Ok(new { message = "Plik zapisany poprawnie", attachmentId = result.Id, url = secureUrl, fileName = result.FileName });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("upload-batch")]
        public async Task<IActionResult> UploadBatch([FromForm] Guid incidentId, [FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest(new { message = "Nie przesłano żadnych plików." });

            try
            {
                var uploaded = new List<object>();
                foreach (var file in files)
                {
                    var result = await _attachmentService.UploadFileAsync(file, incidentId);
                    uploaded.Add(new
                    {
                        attachmentId = result.Id,
                        url = _blobStorage.GetSecureFileUrl(result.PathToFile, "incidents"),
                        fileName = result.FileName
                    });
                }
                return Ok(new { message = $"Zapisano {uploaded.Count} załącznik(ów).", attachments = uploaded });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("incident/{incidentId}")]
        public async Task<IActionResult> GetByIncident(Guid incidentId)
        {
            var list = await _attachmentService.GetByIncidentAsync(incidentId);
            var withUrls = list.Select(a => new
            {
                a.Id,
                a.FileName,
                a.ContentType,
                a.FileSizeBytes,
                a.UploadedAt,
                url = _blobStorage.GetSecureFileUrl(a.Url, "incidents")
            });
            return Ok(withUrls);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _attachmentService.DeleteAsync(id);
            return ok ? Ok(new { message = "Załącznik usunięty." }) : NotFound();
        }
    }
}
